using System;
using System.Collections.Generic;
using BlackFace.Libraries.Modules.UIModule;
using BrunoMikoski.AnimationSequencer;
using DG.Tweening;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;

public class ScreenDontDestroyController : FeatureBaseController
{
    // public virtual void Open()
    // {
    //     if (!isClose) return;
    //     Hide(false);
    // }

    [SerializeField] [TabGroup("Cấu hình riêng")]
    private List<DOTweenAnimation> _tweensOpen = new();

    [SerializeField] [TabGroup("Cấu hình riêng")]
    private AnimationSequencerController _sequencerController;

    private bool isInit;

    // protected override void AnimUI()
    // {
    //
    // }

    public void Hide(bool isHide)
    {
        if (!isInit && !isHide)
        {
            isInit = true;
            return;
        }

        isClose = isHide;
        //this.ThisCanvasGroup.DOKill();
        //MainUI.DOKill();
        //MainBackground?.DOKill();
        var alpha = isHide ? 0 : 1;
        if (isHide)
        {
            ThisCanvasGroup.blocksRaycasts = false;
            AnimCloseUI(() =>
            {
                ThisCanvasGroup.alpha = alpha;
                WhenHide();
            });
        }
        else
        {
            ThisCanvasGroup.alpha = alpha;
            ThisCanvasGroup.blocksRaycasts = true;
            WhenShow();
            foreach (var tween in _tweensOpen) tween.DORestart();

            if (_sequencerController)
                _sequencerController.Play();
        }
    }

    protected virtual void WhenHide()
    {
        EventManager.EmitEvent(nameof(EventName.OnCloseFeature));
    }

    protected virtual void WhenShow()
    {
        AnimOpenUI();
        // removed: TutorialContainer.HideFinger (gameplay removed)
        EventManager.EmitEvent(nameof(EventName.OnShowFeature));
    }

    public override void CloseMe(Action completeAction = null)
    {
        Hide(true);
    }
}