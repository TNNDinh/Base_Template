using Ezg.Feature.Shared.Systems;

//using System;
//using System.Collections.Generic;
//using Ezg.Core.Utils;
//using Ezg.Package.Pooling;
//using BlackFace.Libraries.Modules.UIModule;
//using DG.Tweening;
//using Sirenix.OdinInspector;
//using UnityEngine;

//namespace Assets.Scripts._2.BUS.Features.UnlockFeature
//{
//    [Serializable]
//    public class UnlockFeatureProperty
//    {
//        public GameEnums.Features feature;

//        public UnlockFeatureProperty(GameEnums.Features feature)
//        {
//            this.feature = feature;
//        }
//    }

//    [Serializable]
//    public class UnlockSkillProperty : UnlockFeatureProperty
//    {
//        public EnumBase.SkillTypes SkillType;
//        public int SkillId;

//        public UnlockSkillProperty(GameEnums.Features feature, EnumBase.SkillTypes type, int skillId) : base(feature)
//        {
//            this.feature = feature;
//            SkillType = type;
//            SkillId = skillId;
//        }
//    }

//    public class UnlockFeatureController : FeatureBaseController
//    {
//        [SerializeField]
//        [TabGroup("Cấu hình")]
//        private RectTransform _featureParent;

//        [SerializeField]
//        [TabGroup("Cấu hình")]
//        private UnlockFaetureItemController _featureObjectTemplate;

//        [Header("Setup Move")]
//        [SerializeField]
//        [TabGroup("Cấu hình")]
//        private float duration = 0.5f;

//        [SerializeField]
//        [TabGroup("Cấu hình")]
//        private float scale = 0.2f;

//        [SerializeField]
//        [TabGroup("Cấu hình")]
//        private Ease easeMove;

//        [SerializeField]
//        [TabGroup("Cấu hình")]
//        private GameObject[] objects;

//        [SerializeField]
//        [TabGroup("Cấu hình")]
//        private GameObject block;

//        [SerializeField]
//        [TabGroup("Cấu hình")]
//        private Vector2 positionStart;

//        private UnlockFeatureModel config;

//        private List<UnlockFeatureProperty> property;
//        private Transform target;

//        public override void LoadData(object data)
//        {
//            base.LoadData(data);

//            property = (List<UnlockFeatureProperty>)data;
//            UpdateView();
//        }

//        private void UpdateView()
//        {
//            SetActiveObject(true);
//            // rectIcon.DOAnchorPos(positionStart, 0).SetUpdate(true);
//            // rectIcon.DOScale(Vector3.one, 0).SetUpdate(true);
//            // config = DataManager.UnlockFeature.GetConfigByType(property.feature);
//            // icon.sprite = property is UnlockSkillProperty skillPop
//            //     ? skillPop.SkillType == EnumBase.SkillTypes.Passive
//            //         ? PlayerResource.GetPassiveImage(skillPop.SkillId)
//            //         : PlayerResource.GetSkillImage(skillPop.SkillId)
//            //     : property.feature.GetIconFeature();
//            // textNameFeature.text = GameSystems.Localize(property.feature.ToString());

//            property.ForEach(x =>
//                PoolingManager.Instantiate<UnlockFaetureItemController>(_featureObjectTemplate,
//                    _featureParent).InitData(x));

//            // var obj = GameObject.FindGameObjectWithTag(config.tag);
//            // target = obj.transform;
//        }

//        public override void CloseMe(Action completeAction = null)
//        {
//            base.CloseMe();
//            SetActiveObject(false);
//            TutorialContainer.CheckTutorial(Location.UnlockFeature);
//            // completeAction = () =>
//            // {
//            //     UIManager.Instance.DelayMethod(0.2f,
//            //         () => { EventManager.EmitEvent(nameof(EventName.UnlockFeature)); });
//            // };

//            // rectIcon.DOScale(scale, duration).SetUpdate(true).OnComplete(() =>
//            // {
//            //     PlayerDataManager.UnlockFeature.AddFeatureShowed(property.feature);
//            //     base.CloseMe(completeAction);
//            // });
//            // rectIcon.DOMove(target.position, duration).SetEase(easeMove).SetUpdate(true);
//        }

//        private void SetActiveObject(bool isActive)
//        {
//            foreach (var obj in objects) obj.SetActive(isActive);

//            block.SetActive(!isActive);
//        }
//    }
//}