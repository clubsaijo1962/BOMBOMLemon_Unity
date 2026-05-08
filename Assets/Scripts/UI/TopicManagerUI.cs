using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    public class TopicManagerUI : MonoBehaviour
    {
        [Header("References")]
        public RectTransform listContent;
        public InputField    searchField;
        public Button        addButton;
        public TopicFormUI   formPanel;

        // ── colour palette (duplicates builder to avoid editor deps) ──────
        static readonly Color DarkBrown  = new Color(0.15f, 0.10f, 0.00f);
        static readonly Color SubBrown   = new Color(0.50f, 0.36f, 0.00f);
        static readonly Color LimeGreen  = new Color(0.25f, 0.60f, 0.08f);
        static readonly Color GreenBtn   = new Color(0.18f, 0.55f, 0.12f);
        static readonly Color RedBtn     = new Color(0.75f, 0.18f, 0.12f);
        static readonly Color CardBg     = new Color(1f, 1f, 1f, 0.30f);
        static readonly Color HdrBg      = new Color(0.90f, 0.78f, 0.20f, 0.50f);
        static readonly Color EyeOnCol   = LimeGreen;
        static readonly Color EyeOffCol  = new Color(0.65f, 0.65f, 0.65f);

        string _search = "";

        void OnEnable()
        {
            if (searchField != null)
            {
                searchField.onValueChanged.RemoveAllListeners();
                searchField.onValueChanged.AddListener(OnSearch);
                _search = searchField.text;
            }
            if (addButton != null)
            {
                addButton.onClick.RemoveAllListeners();
                addButton.onClick.AddListener(OnAdd);
            }
            if (GameManager.Instance != null)
                GameManager.Instance.OnPhaseChanged += OnStateChanged;
            Rebuild();
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPhaseChanged -= OnStateChanged;
        }

        void OnStateChanged(GamePhase _) => Rebuild();
        void OnSearch(string v) { _search = v; Rebuild(); }
        void OnAdd() { formPanel?.OpenForNew(); }

        public void Rebuild()
        {
            if (listContent == null) return;
            // Destroy old rows
            foreach (Transform c in listContent) Destroy(c.gameObject);

            var gm = GameManager.Instance;
            if (gm == null) return;

            string q = (_search ?? "").Trim();

            // ── Default topics toggle ─────────────────────────────────────────
            AddSectionHeader("デフォルトのお題");
            AddToggleRow("デフォルトのお題を使用する", gm.UseDefaultTopics, v => gm.SetUseDefaultTopics(v));

            if (gm.UseDefaultTopics)
            {
                // Show All / Hide All row
                AddBtnRow(
                    ("すべて表示", GreenBtn,  () => gm.ShowAllDefaultTopics()),
                    ("すべて非表示", RedBtn,  () => gm.HideAllDefaultTopics()));

                // Default topic rows filtered by search
                foreach (var t in TopicData.AllTopics)
                {
                    if (!Matches(t.Japanese, q) && !Matches(t.English, q)) continue;
                    bool hidden = gm.HiddenDefaultTopicIDs.Contains(t.Id);
                    AddDefaultTopicRow(t, hidden);
                }
            }

            // ── Custom topics ─────────────────────────────────────────────────
            AddSectionHeader("カスタムお題");

            var filtered = new List<UserTopic>();
            foreach (var ut in gm.UserTopics)
                if (Matches(ut.Japanese, q) || Matches(ut.English, q) || string.IsNullOrEmpty(q))
                    filtered.Add(ut);

            if (filtered.Count == 0)
            {
                AddInfoRow("カスタムお題はありません。＋で追加できます。");
            }
            else
            {
                foreach (var ut in filtered)
                    AddUserTopicRow(ut);
            }

            // ── Zero topics warning ───────────────────────────────────────────
            if (gm.GetActiveTopics().Count == 0)
                AddWarningRow("お題が 0 件です。ゲームを始める前にお題を有効にしてください。");
        }

        // ── Row builders ──────────────────────────────────────────────────────

        void AddSectionHeader(string text)
        {
            var go = MakeRow(34);
            go.AddComponent<Image>().color = HdrBg;
            MakeLabel(go, text, 12, FontStyle.Bold, DarkBrown, TextAnchor.MiddleLeft, new Vector2(10, 0), new Vector2(-10, 0));
        }

        void AddToggleRow(string label, bool value, System.Action<bool> onChange)
        {
            var go = MakeRow(44);
            go.AddComponent<Image>().color = CardBg;
            MakeLabel(go, label, 13, FontStyle.Normal, DarkBrown, TextAnchor.MiddleLeft, new Vector2(10, 0), new Vector2(-60, 0));

            var tGo = new GameObject("Toggle"); tGo.transform.SetParent(go.transform, false);
            var tRT = tGo.AddComponent<RectTransform>();
            tRT.anchorMin = new Vector2(1, 0.5f); tRT.anchorMax = new Vector2(1, 0.5f);
            tRT.pivot = new Vector2(1, 0.5f); tRT.anchoredPosition = new Vector2(-10, 0);
            tRT.sizeDelta = new Vector2(44, 28);
            var tog = tGo.AddComponent<Toggle>();
            tog.isOn = value;

            // Toggle background
            var bgGo = new GameObject("Bg"); bgGo.transform.SetParent(tGo.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>(); bgImg.color = value ? LimeGreen : EyeOffCol;
            tog.targetGraphic = bgImg;

            // Knob
            var knobGo = new GameObject("Knob"); knobGo.transform.SetParent(tGo.transform, false);
            var knobRT = knobGo.AddComponent<RectTransform>();
            knobRT.anchorMin = knobRT.anchorMax = new Vector2(value ? 1f : 0f, 0.5f);
            knobRT.pivot = new Vector2(value ? 1f : 0f, 0.5f);
            knobRT.anchoredPosition = new Vector2(value ? -2f : 2f, 0);
            knobRT.sizeDelta = new Vector2(22, 22);
            knobGo.AddComponent<Image>().color = Color.white;

            tog.onValueChanged.AddListener(v =>
            {
                bgImg.color = v ? LimeGreen : EyeOffCol;
                onChange?.Invoke(v);
            });
        }

        void AddBtnRow((string label, Color col, System.Action cb) a, (string label, Color col, System.Action cb) b)
        {
            var go = MakeRow(38);
            var hg = go.AddComponent<HorizontalLayoutGroup>();
            hg.spacing = 8; hg.childForceExpandWidth = true; hg.childForceExpandHeight = true;
            hg.padding = new RectOffset(10, 10, 5, 5);
            MakeSmallBtn(go, a.label, a.col, a.cb);
            MakeSmallBtn(go, b.label, b.col, b.cb);
        }

        void AddDefaultTopicRow(Topic t, bool hidden)
        {
            var go = MakeRow(38);
            go.AddComponent<Image>().color = new Color(1, 1, 1, hidden ? 0.08f : 0.20f);

            MakeLabel(go, t.Japanese, 12, FontStyle.Normal,
                hidden ? EyeOffCol : DarkBrown,
                TextAnchor.MiddleLeft, new Vector2(10, 0), new Vector2(-50, 0));

            // Eye toggle button
            var eyeGo = new GameObject("Eye"); eyeGo.transform.SetParent(go.transform, false);
            var eyeRT = eyeGo.AddComponent<RectTransform>();
            eyeRT.anchorMin = new Vector2(1, 0.5f); eyeRT.anchorMax = new Vector2(1, 0.5f);
            eyeRT.pivot = new Vector2(1, 0.5f); eyeRT.anchoredPosition = new Vector2(-6, 0);
            eyeRT.sizeDelta = new Vector2(36, 28);
            eyeGo.AddComponent<Image>().color = Color.clear;
            var eyeBtn = eyeGo.AddComponent<Button>(); NoNav(eyeBtn);

            var eyeTxtGo = new GameObject("T"); eyeTxtGo.transform.SetParent(eyeGo.transform, false);
            var eyeTxtRT = eyeTxtGo.AddComponent<RectTransform>();
            eyeTxtRT.anchorMin = Vector2.zero; eyeTxtRT.anchorMax = Vector2.one; eyeTxtRT.offsetMin = eyeTxtRT.offsetMax = Vector2.zero;
            var eyeTxt = eyeTxtGo.AddComponent<Text>();
            eyeTxt.text = hidden ? "👁" : "✓";
            eyeTxt.fontSize = 16; eyeTxt.alignment = TextAnchor.MiddleCenter;
            eyeTxt.color = hidden ? EyeOffCol : EyeOnCol;

            int capturedId = t.Id;
            bool capturedHidden = hidden;
            eyeBtn.onClick.AddListener(() =>
            {
                GameManager.Instance?.SetHideDefaultTopic(capturedId, !capturedHidden);
            });
        }

        void AddUserTopicRow(UserTopic ut)
        {
            var go = MakeRow(44);
            go.AddComponent<Image>().color = new Color(0.88f, 0.97f, 0.78f, 0.35f);

            MakeLabel(go, ut.Japanese, 12, FontStyle.Bold, DarkBrown, TextAnchor.MiddleLeft,
                new Vector2(10, 0), new Vector2(-90, 0));

            // Edit + Delete buttons
            var btnRow = new GameObject("Btns"); btnRow.transform.SetParent(go.transform, false);
            var btnRT = btnRow.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(1, 0.5f); btnRT.anchorMax = new Vector2(1, 0.5f);
            btnRT.pivot = new Vector2(1, 0.5f); btnRT.anchoredPosition = new Vector2(-6, 0);
            btnRT.sizeDelta = new Vector2(82, 30);
            var hg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hg.spacing = 4; hg.childForceExpandWidth = true; hg.childForceExpandHeight = true;

            var editCaptured = ut;
            MakeSmallBtn(btnRow, "編集", GreenBtn, () => formPanel?.OpenForEdit(editCaptured));
            MakeSmallBtn(btnRow, "削除", RedBtn,   () => GameManager.Instance?.DeleteUserTopic(editCaptured.Id));
        }

        void AddInfoRow(string text)
        {
            var go = MakeRow(40);
            MakeLabel(go, text, 11, FontStyle.Normal, SubBrown, TextAnchor.MiddleCenter, new Vector2(10, 0), new Vector2(-10, 0));
        }

        void AddWarningRow(string text)
        {
            var go = MakeRow(50);
            go.AddComponent<Image>().color = new Color(1f, 0.3f, 0.1f, 0.15f);
            MakeLabel(go, text, 12, FontStyle.Bold, new Color(0.8f, 0.1f, 0.1f), TextAnchor.MiddleCenter, new Vector2(10, 0), new Vector2(-10, 0));
        }

        // ── Row / widget factories ─────────────────────────────────────────────

        GameObject MakeRow(float height)
        {
            var go = new GameObject("Row"); go.transform.SetParent(listContent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(0, height);
            var le = go.AddComponent<LayoutElement>(); le.minHeight = height;
            return go;
        }

        void MakeLabel(GameObject parent, string text, int size, FontStyle style, Color color,
            TextAnchor align, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject("L"); go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            var t = go.AddComponent<Text>();
            t.text = text; t.fontSize = size; t.fontStyle = style;
            t.color = color; t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
        }

        void MakeSmallBtn(GameObject parent, string label, Color bg, System.Action cb)
        {
            var go = new GameObject(label); go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>(); NoNav(btn);
            btn.onClick.AddListener(() => cb?.Invoke());
            var lGo = new GameObject("L"); lGo.transform.SetParent(go.transform, false);
            var lRT = lGo.AddComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one; lRT.offsetMin = lRT.offsetMax = Vector2.zero;
            var t = lGo.AddComponent<Text>();
            t.text = label; t.fontSize = 11; t.fontStyle = FontStyle.Bold;
            t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        }

        static bool Matches(string haystack, string q)
            => string.IsNullOrEmpty(q) || (haystack != null && haystack.Contains(q));

        static void NoNav(Button btn) { var n = btn.navigation; n.mode = Navigation.Mode.None; btn.navigation = n; }
    }
}
