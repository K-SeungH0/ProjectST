using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class UIForm : MonoBehaviour
{
    protected Action _closeEvent;
    private bool _isSubscribedToLanguageChange = false;
    private TMP_Text[] _cachedTextComponents;

    public virtual void OnDispatch(string eventID, object param)
	{

	}

    public virtual void Awake()
    {
    }

    public virtual void BindComponents()
    {
    }

    public virtual void BindEvents()
    {
    }

    public virtual void Open()
    {
        SubscribeToLanguageChange();
        RegisterAllTextComponents();
        DispatchHandler.RegisterListener(this);
    }

    public virtual void Init()
    {
    }

    public virtual void Close()
    {
        UnsubscribeFromLanguageChange();
        UnregisterAllTextComponents();
        UIFormManager.Instance.CloseUIForm(this);
		_closeEvent?.Invoke();
		_closeEvent = null;
        DispatchHandler.UnregisterListener(this);
    }

    public virtual void Refresh()
    {
    }

    public void AddCloseEvent(Action action)
    {
		_closeEvent += action;
    }
    
    /// <summary>
    /// 언어 변경 시 호출됩니다. 하위 클래스에서 오버라이드하여 텍스트를 갱신하세요.
    /// </summary>
    protected virtual void OnLanguageChanged(Language language)
    {
        // 기본 구현: 동적 텍스트를 포함한 모든 TMP_Text 재스캔 및 등록
        GameMain.Instance.StartCoroutine(ReRegisterAllTextComponentsAfterFrame());
    }
    
    private void SubscribeToLanguageChange()
    {
        if (_isSubscribedToLanguageChange)
            return;
        
        if (LocalizationManager.Instance.IsValid())
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            _isSubscribedToLanguageChange = true;
        }
    }
    
    private void UnsubscribeFromLanguageChange()
    {
        if (!_isSubscribedToLanguageChange)
            return;
        
        if (LocalizationManager.Instance.IsValid())
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            _isSubscribedToLanguageChange = false;
        }
    }
    
    protected void RegisterAllTextComponents()
    {
        if (LocalizationManager.Instance.IsNull())
            return;
        
        // 이 UI의 모든 TMP_Text 컴포넌트 찾기
        _cachedTextComponents = GetComponentsInChildren<TMP_Text>(true);
        
        foreach (var tmpText in _cachedTextComponents)
        {
            if (tmpText.IsValid())
                LocalizationManager.Instance.RegisterTextComponent(tmpText);
        }
    }
    
    private void UnregisterAllTextComponents()
    {
        if (LocalizationManager.Instance.IsNull() || _cachedTextComponents.IsNull())
            return;
        
        foreach (var tmpText in _cachedTextComponents)
        {
            if (tmpText.IsValid())
                LocalizationManager.Instance.UnregisterTextComponent(tmpText);
        }
    }
    
    /// <summary>
    /// 언어 변경 시 동적 텍스트를 포함한 모든 TMP_Text를 재스캔하여 등록합니다.
    /// </summary>
    private IEnumerator ReRegisterAllTextComponentsAfterFrame()
    {
        // 한 프레임 대기 (동적 텍스트 생성 완료 대기)
        yield return null;
        
        if (LocalizationManager.Instance.IsNull())
            yield break;
        
        // 새로 생성된 TMP_Text들도 포함해서 다시 스캔
        var allTextComponents = GetComponentsInChildren<TMP_Text>(true);
        
        foreach (var tmpText in allTextComponents)
        {
            if (tmpText.IsValid())
            {
                // 이미 등록된 텍스트는 중복 등록되지 않음 (LocalizationManager에서 처리)
                LocalizationManager.Instance.RegisterTextComponent(tmpText);
            }
        }
        
        // 캐시된 텍스트 컴포넌트 목록 업데이트
        _cachedTextComponents = allTextComponents;
    }
}
