using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFormManager : MonoSingleton_Global<UIFormManager>
{
    [SerializeField] private CanvasScaler _scaler;
    [SerializeField] private Transform _parent;
    [SerializeField] private Transform _globalParent;

    private Dictionary<Type, UIForm> _openedUIForms = new Dictionary<Type, UIForm>();
	private Dictionary<Type, UIForm> _openedUIForms_Global = new Dictionary<Type, UIForm>();

	bool isTouchBegin = false;
	float touchTime = 0;


	protected override void Awake()
    {
        base.Awake();
        InitParent();
    }

	private void Update()
	{
#if DEV_QA

#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.F5))
			OpenUIForm<UI_OVERLAY_CHEAT>();
#else
        if (Input.touchCount == 3)
		{
			if (isTouchBegin == false)
			{
				isTouchBegin = true;
				touchTime = Time.unscaledTime;
			}
			else
			{
				if (Time.unscaledTime - touchTime >= 2)
				{

					isTouchBegin = false;
					touchTime = 0;
					OpenUIForm<UI_OVERLAY_CHEAT>();
				}
			}
		}
		else
		{
			isTouchBegin = false;
			touchTime = 0;
		}
#endif

#endif
	}

	public UIForm BindUIForm(GameObject root)
    {
        if(root.IsValid())
        {
            var uiForm = root.GetComponent<UIForm>();
            if(uiForm)
            {
                uiForm.BindComponents();
				uiForm.BindEvents();
				uiForm.Open();
				uiForm.Init();

                return uiForm;
			}
            else
            {
				LogManager.LogError($"[UIFormManager] BindUIForm Error Cannot find UIForm : {root.name}");
			}
		}
        else
        {
			LogManager.LogError("[UIFormManager] BindUIForm Error GameObject is null");
		}

        return null;
    }
	
	/// <summary>
	/// 이미 생성된 UIForm을 Global UI로 등록 (씬 전환 시 유지)
	/// </summary>
	public void BindUIForm_Global(UIForm uiForm)
	{
		if (uiForm.IsNull())
		{
			LogManager.LogError("[UIFormManager] BindUIForm_Global Error UIForm is null");
			return;
		}
		
		Type formType = uiForm.GetType();
		
		if (_openedUIForms_Global.ContainsKey(formType))
		{
			LogManager.LogWarning($"[UIFormManager] BindUIForm_Global: '{formType.Name}'이(가) 이미 등록되어 있습니다.");
			return;
		}
		
		_openedUIForms_Global.Add(formType, uiForm);
        uiForm.transform.SetParent(_globalParent);
        LogManager.Log($"[UIFormManager] BindUIForm_Global: '{formType.Name}' Global UI로 등록 완료");
	}

    public T OpenUIForm<T>() where T : UIForm
    {
        var findForm = FindUIForm<T>();
        if (findForm.IsValid())
        {
            LogManager.LogWarning($"[UIFormManager] '{typeof(T).Name}'은(는) 이미 열려있습니다.");
            // 이미 열려있다면 맨 위로 올려주는 로직 등을 추가할 수 있습니다.
            // existingForm.transform.SetAsLastSibling();
            return findForm;
        }

        GameObject instanceGameObject = ResourceManager.Instance.Instantiate($"UI/PREFABS/{typeof(T).Name}", _parent);
        if (instanceGameObject.IsNull())
        {
            LogManager.LogError($"[UIFormManager] '{typeof(T).Name}' 프리팹을 로드하거나 생성하는 데 실패했습니다.");
            return null;
        }

        var uiForm = instanceGameObject.GetComponentOrAdd<T>();
        if (uiForm.IsNull())
        {
            ResourceManager.Destroy(instanceGameObject);
            return null;
        }

        uiForm.BindComponents();
        uiForm.BindEvents();
        uiForm.Open();
        uiForm.Init();

        _openedUIForms.Add(typeof(T), uiForm);
        return uiForm;
    }

	/// <summary> 기본 UI 시스템 외 씬 전환이나 CloseAll로도 Close되지않는 UI Open. ex) 로딩창 </summary>
	public T OpenUIForm_Global<T>() where T : UIForm
	{
		var findForm = FindUIForm<T>();
		if (findForm.IsValid())
		{
			LogManager.LogWarning($"[UIFormManager] '{typeof(T).Name}'은(는) 이미 열려있습니다.");
			// 이미 열려있다면 맨 위로 올려줍니다 (Global UI는 항상 최상위)
			findForm.transform.SetAsLastSibling();
			return findForm;
		}

		GameObject instanceGameObject = ResourceManager.Instance.Instantiate($"UI/PREFABS/{typeof(T).Name}", _globalParent);
		if (instanceGameObject.IsNull())
		{
			LogManager.LogError($"[UIFormManager] '{typeof(T).Name}' 프리팹을 로드하거나 생성하는 데 실패했습니다.");
			return null;
		}

		var uiForm = instanceGameObject.GetComponentOrAdd<T>();
		if (uiForm.IsNull())
		{
			ResourceManager.Destroy(instanceGameObject);
			return null;
		}

		uiForm.BindComponents();
		uiForm.BindEvents();
		uiForm.Open();
		uiForm.Init();

		// Global UI는 항상 최상위에 표시
		uiForm.transform.SetAsLastSibling();

		_openedUIForms_Global.Add(typeof(T), uiForm);
		return uiForm;
	}

	public void CloseUIForm<T>() where T : UIForm
	{
		Type formType = typeof(T);

		if (_openedUIForms.TryGetValue(formType, out UIForm uiForm))
            uiForm.Close();
        else if(_openedUIForms_Global.TryGetValue(formType, out UIForm uiFormGlobal))
			uiFormGlobal.Close();
		else
			LogManager.LogWarning($"[UIFormManager] 닫으려는 UI '{formType.Name}'이(가) 열려있지 않습니다.");
	}

	public void CloseUIForm(UIForm uiFormToClose)
    {
        if (uiFormToClose.IsNull())
        {
            LogManager.LogWarning($"[UIFormManager] 닫으려는 UI가 유효하지 않습니다.");
            return;
        }

        Type formType = uiFormToClose.GetType();

        if (_openedUIForms.TryGetValue(formType, out UIForm uiForm))
        {
            _openedUIForms.Remove(formType);
            ResourceManager.Destroy(uiFormToClose.gameObject);
        }
        else if (_openedUIForms_Global.TryGetValue(formType, out UIForm uiFormGlobal))
        {
            _openedUIForms_Global.Remove(formType);
            ResourceManager.Destroy(uiFormToClose.gameObject);
        }
        else
        {
            LogManager.LogWarning($"[UIFormManager] 닫으려는 UI '{formType.Name}'이(가) 열려있지 않습니다.");
        }
    }

    public T FindUIForm<T>() where T : UIForm
    {
        if (_openedUIForms.TryGetValue(typeof(T), out UIForm form))
        {
            if (form.IsValid())
                return form as T;
            else
                _openedUIForms.Remove(typeof(T));
		}
        else if (_openedUIForms_Global.TryGetValue(typeof(T), out UIForm formGlobal))
		{
			if (formGlobal.IsValid())
				return formGlobal as T;
			else
				_openedUIForms_Global.Remove(typeof(T));
		}

		return null;
    }

    public void CloseAllUIForm()
    {
        foreach (var form in _openedUIForms.Values.ToList())
        {
            form.Close();
            ResourceManager.Destroy(form.gameObject);
        }
        _openedUIForms.Clear();
    }

    public float getHorizontalResolutionRatio()
    {
        if (_scaler.IsValid())
            return (float)Screen.width / _scaler.referenceResolution.x;
        return 1f;

	}

    private void InitParent() 
    {
        if (_parent.IsNull())
        {
            var canvas = GetComponentInChildren<Canvas>();
            if (canvas.IsValid())
                _parent = canvas.transform;
        }
    }
}