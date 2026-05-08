using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    public class TopicFormUI : MonoBehaviour
    {
        [Header("References")]
        public GameObject  panel;
        public Text        titleText;
        public InputField  jpField;
        public InputField  enField;
        public Dropdown    categoryDropdown;
        public Button      saveButton;
        public Button      cancelButton;

        private int _editingId;   // 0 = new topic, negative = editing existing

        static readonly string[] CategoryNames =
        {
            "必要性 (Necessary)",
            "人気 (Popular)",
            "美味しさ (Delicious)",
            "高さ (Expensive)",
            "悲しさ (Sad)",
            "驚き (Surprising)",
            "ムカつき (Annoying)",
            "嬉しさ (Happy)",
            "大きさ (Big)",
            "可愛さ (Cute)",
        };

        void Awake()
        {
            if (categoryDropdown != null)
            {
                categoryDropdown.ClearOptions();
                categoryDropdown.AddOptions(new List<string>(CategoryNames));
            }
            saveButton?.onClick.AddListener(OnSave);
            cancelButton?.onClick.AddListener(OnCancel);
        }

        public void OpenForNew()
        {
            _editingId = 0;
            if (titleText)    titleText.text = "カスタムお題を追加";
            if (jpField)      jpField.text   = "";
            if (enField)      enField.text   = "";
            if (categoryDropdown) categoryDropdown.value = 0;
            panel?.SetActive(true);
        }

        public void OpenForEdit(UserTopic ut)
        {
            _editingId = ut.Id;
            if (titleText)    titleText.text = "お題を編集";
            if (jpField)      jpField.text   = ut.Japanese ?? "";
            if (enField)      enField.text   = ut.English  ?? "";
            if (categoryDropdown) categoryDropdown.value = (int)ut.Category;
            panel?.SetActive(true);
        }

        void OnSave()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            string jp = jpField?.text.Trim() ?? "";
            string en = enField?.text.Trim() ?? "";
            var cat = categoryDropdown != null
                ? (TopicCategory)categoryDropdown.value
                : TopicCategory.Necessary;

            if (string.IsNullOrEmpty(jp)) return;

            if (_editingId == 0)
                gm.AddUserTopic(jp, en, cat);
            else
                gm.UpdateUserTopic(_editingId, jp, en, cat);

            panel?.SetActive(false);
        }

        void OnCancel() => panel?.SetActive(false);
    }
}
