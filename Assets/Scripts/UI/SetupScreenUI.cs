using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    // Setup screen: player name list, add/remove, start game
    public class SetupScreenUI : MonoBehaviour
    {
        // Assigned by TitleSceneBuilder
        public Text       headerText;
        public RectTransform playerListContent;   // Content of ScrollRect
        public Button     addButton;
        public Button     startButton;
        public Text       startButtonLabel;
        public Text       playerCountLabel;

        readonly List<GameObject> _rows = new();

        void OnEnable()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.OnPhaseChanged += OnStateChanged;
            Refresh();
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPhaseChanged -= OnStateChanged;
        }

        void OnStateChanged(GamePhase _) => Refresh();

        public void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (headerText)       headerText.text    = "プレイヤー設定";
            if (playerCountLabel) playerCountLabel.text = $"{gm.PlayerCount}人";
            if (startButtonLabel) startButtonLabel.text = "スタート！";
            if (startButton)      startButton.interactable = gm.PlayerCount >= 2;
            if (addButton)        addButton.gameObject.SetActive(gm.PlayerCount < 24);

            RebuildRows(gm);
        }

        void RebuildRows(GameManager gm)
        {
            foreach (var r in _rows) if (r) Destroy(r);
            _rows.Clear();

            for (int i = 0; i < gm.PlayerCount; i++)
                _rows.Add(BuildRow(gm, i));
        }

        GameObject BuildRow(GameManager gm, int idx)
        {
            var dark = gm.DarkColor;

            var row = new GameObject($"Row{idx}");
            row.transform.SetParent(playerListContent, false);
            var rt = row.AddComponent<RectTransform>(); rt.sizeDelta = new Vector2(0, 52);
            var le = row.AddComponent<LayoutElement>(); le.minHeight = 52;
            var hg = row.AddComponent<HorizontalLayoutGroup>();
            hg.spacing = 8; hg.padding = new RectOffset(0, 0, 6, 6);
            hg.childForceExpandWidth = false; hg.childForceExpandHeight = true;

            // ── Index label ───
            AddLabel(row, $"{idx + 1}", 15, FontStyle.Bold, dark, 28, TextAnchor.MiddleCenter, 0);

            // ── Input field ───
            var fieldGo = new GameObject("Field");
            fieldGo.transform.SetParent(row.transform, false);
            var fRT  = fieldGo.AddComponent<RectTransform>(); fRT.sizeDelta = new Vector2(0, 40);
            var fLE  = fieldGo.AddComponent<LayoutElement>();  fLE.flexibleWidth = 1;
            var fImg = fieldGo.AddComponent<Image>(); fImg.color = new Color(1,1,1,0.55f);
            var field = fieldGo.AddComponent<InputField>();
            field.text = idx < gm.PlayerNames.Count ? gm.PlayerNames[idx] : "";
            field.characterLimit = 20;

            var txtGo = MakeChildRect(fieldGo, "Text");
            SetRectPad(txtGo, 8, 4);
            var txt = txtGo.AddComponent<Text>();
            txt.fontSize = 15; txt.color = dark; txt.alignment = TextAnchor.MiddleLeft;
            field.textComponent = txt;

            var phGo = MakeChildRect(fieldGo, "PH");
            SetRectPad(phGo, 8, 4);
            var ph = phGo.AddComponent<Text>();
            ph.text = $"プレイヤー{idx + 1}"; ph.fontSize = 15;
            ph.color = new Color(0.55f,0.55f,0.55f,0.8f); ph.alignment = TextAnchor.MiddleLeft;
            field.placeholder = ph;

            int cap = idx;
            field.onEndEdit.AddListener(val =>
            {
                var g = GameManager.Instance;
                if (g != null && cap < g.PlayerNames.Count) g.PlayerNames[cap] = val;
            });

            // ── Remove button ───
            if (gm.PlayerCount > 2)
            {
                int capR = idx;
                var rmGo = new GameObject("Rm");
                rmGo.transform.SetParent(row.transform, false);
                var rmRT = rmGo.AddComponent<RectTransform>(); rmRT.sizeDelta = new Vector2(36,36);
                var rmLE = rmGo.AddComponent<LayoutElement>(); rmLE.minWidth = 36; rmLE.flexibleWidth = 0;
                var rmImg = rmGo.AddComponent<Image>(); rmImg.color = new Color(0.9f,0.25f,0.25f,0.85f);
                var rmBtn = rmGo.AddComponent<Button>(); DisableNav(rmBtn);
                var rmX = MakeChildRect(rmGo, "X");
                SetRectFill(rmX);
                var rmTxt = rmX.AddComponent<Text>();
                rmTxt.text = "−"; rmTxt.fontSize = 22; rmTxt.fontStyle = FontStyle.Bold;
                rmTxt.color = Color.white; rmTxt.alignment = TextAnchor.MiddleCenter;
                rmBtn.onClick.AddListener(() =>
                {
                    SoundManager.Instance?.PlaySE("click");
                    GameManager.Instance?.SetupRemovePlayer(capR);
                });
            }
            return row;
        }

        // ── Button callbacks ────────────────────────────────────────────────
        public void OnAddPlayer()
        {
            SoundManager.Instance?.PlaySE("click");
            GameManager.Instance?.SetupAddPlayer();
        }

        public void OnStart()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.PlayerCount < 2) return;
            SoundManager.Instance?.PlaySE("click");
            gm.StartGame();
        }

        // ── Helpers ─────────────────────────────────────────────────────────
        static void AddLabel(GameObject parent, string text, int fontSize, FontStyle style,
            Color color, float width, TextAnchor align, float flexW)
        {
            var go = new GameObject("Lbl");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>(); rt.sizeDelta = new Vector2(width, 0);
            var le = go.AddComponent<LayoutElement>(); le.minWidth = width; le.flexibleWidth = flexW;
            var txt = go.AddComponent<Text>();
            txt.text = text; txt.fontSize = fontSize; txt.fontStyle = style;
            txt.color = color; txt.alignment = align;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        static GameObject MakeChildRect(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        static void SetRectPad(GameObject go, float hPad, float vPad)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(hPad, vPad);
            rt.offsetMax = new Vector2(-hPad, -vPad);
        }

        static void SetRectFill(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void DisableNav(Button btn)
        {
            var n = btn.navigation; n.mode = Navigation.Mode.None; btn.navigation = n;
        }
    }
}
