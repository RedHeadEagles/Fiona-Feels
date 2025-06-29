using System;
using UnityEditor.IMGUI.Controls;
using UnityEditor.U2D.Common;
using UnityEditor.U2D.Layout;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Animation
{
    internal class BoneReparentToolController : BoneTreeWidgetController
    {
        public BoneReparentToolController(IBoneTreeViewModel model, SkinningEvents eventSystem)
            : base(model, eventSystem) { }

        public override bool CanDrag()
        {
            m_SkinningEvents.boneVisibility.Invoke("drag");
            return (m_Model.hasCharacter && m_Model.mode == SkinningMode.Character) ||
                (!m_Model.hasCharacter && m_Model.mode == SkinningMode.SpriteSheet);
        }

        public override bool CanRename()
        {
            m_SkinningEvents.boneVisibility.Invoke("rename");
            return true;
        }
    }

    internal class BoneReparentToolModel : BoneTreeWidgetModel
    {
        public BoneReparentToolModel(SkinningCache cache, IBoneVisibilityToolView view)
        {
            m_SkinningCache = cache;
            m_View = view;
            m_Data = skinningCache.CreateCache<BoneVisibilityToolData>();
        }
    }

    internal class BoneReparentTool : SkeletonToolWrapper
    {
        BoneReparentToolWindow m_View;
        BoneReparentToolModel m_Model;
        private BoneReparentToolController m_Controller;


        public override void Initialize(LayoutOverlay layout)
        {
            if (m_View == null)
            {
                m_View = BoneReparentToolWindow.CreateFromUXML();
            }

            m_Model = new BoneReparentToolModel(skinningCache, m_View);
            m_Controller = new BoneReparentToolController(m_Model, skinningCache.events);
            m_View.GetController = () => m_Controller;
            m_View.GetModel = () => m_Model;
            layout.rightOverlay.Add(m_View);
            m_View.SetHiddenFromLayout(true);
        }

        protected override void OnActivate()
        {
            m_View.SetHiddenFromLayout(false);
            m_Controller.Activate();
            skeletonTool.Activate();
        }

        protected override void OnDeactivate()
        {
            m_View.SetHiddenFromLayout(true);
            m_Controller.Deactivate();
            skeletonTool.Deactivate();
        }

        protected override void OnGUI()
        {
            skeletonTool.mode = mode;
            skeletonTool.editBindPose = editBindPose;
            skeletonTool.DoGUI();
        }
    }

#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    internal partial class BoneReparentToolWindow : VisualElement, IBoneVisibilityToolView
    {
#if ENABLE_UXML_TRAITS
        public class CustomUxmlFactory : UxmlFactory<BoneReparentToolWindow, UxmlTraits> { }
#endif

        BoneReparentToolView m_ToolView;
        public Func<IBoneTreeViewModel> GetModel = () => null;
        public Func<BoneTreeWidgetController> GetController = () => null;

        static internal BoneReparentToolWindow CreateFromUXML()
        {
            VisualTreeAsset visualTree = ResourceLoader.Load<VisualTreeAsset>("SkinningModule/BoneReparentWindow.uxml");
            BoneReparentToolWindow ve = visualTree.CloneTree().Q("BoneReparentToolWindow") as BoneReparentToolWindow;
            ve.BindElements();
            return ve;
        }

        internal void BindElements()
        {
            m_ToolView = this.Q<BoneReparentToolView>();
            m_ToolView.GetModel = InternalGetModel;
            m_ToolView.GetController = InternalGetController;
            this.styleSheets.Add(ResourceLoader.Load<StyleSheet>("SkinningModule/BoneReparentStyle.uss"));
        }

        IBoneTreeViewModel InternalGetModel()
        {
            return GetModel();
        }

        BoneTreeWidgetController InternalGetController()
        {
            return GetController();
        }

        public void OnBoneSelectionChange(SkeletonSelection skeleton)
        {
            ((IBoneVisibilityToolView)toolView).OnBoneSelectionChange(skeleton);
        }

        public void OnBoneExpandedChange(BoneCache[] bones)
        {
            ((IBoneVisibilityToolView)toolView).OnBoneExpandedChange(bones);
        }

        public void OnBoneNameChanged(BoneCache bone)
        {
            ((IBoneVisibilityToolView)toolView).OnBoneNameChanged(bone);
        }

        public void OnSelectionChange(SkeletonCache skeleton)
        {
            ((IBoneVisibilityToolView)toolView).OnSelectionChange(skeleton);
        }

        BoneReparentToolView toolView
        {
            get { return m_ToolView; }
        }

        public void Deactivate()
        {
            toolView.Deactivate();
        }
    }

#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    internal partial class BoneReparentToolView : BoneVisibilityToolView
    {
#if ENABLE_UXML_TRAITS
        public class CustomUxmlFactory : UxmlFactory<BoneReparentToolView, CustomUxmlTraits> {}
        public class CustomUxmlTraits : UxmlTraits {}
#endif

        protected override VisibilityToolColumnHeader SetupToolColumnHeader()
        {
            MultiColumnHeaderState.Column[] columns = new MultiColumnHeaderState.Column[4];
            columns[0] = new MultiColumnHeaderState.Column
            {
                headerContent = VisibilityTreeViewBase.VisibilityIconStyle.visibilityOnIcon,
                headerTextAlignment = TextAlignment.Center,
                width = 32,
                minWidth = 32,
                maxWidth = 32,
                autoResize = false,
                allowToggleVisibility = true
            };
            columns[1] = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent(TextContent.bone),
                headerTextAlignment = TextAlignment.Center,
                width = 130,
                minWidth = 130,
                autoResize = true,
                allowToggleVisibility = false
            };

            columns[2] = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent(TextContent.depth),
                headerTextAlignment = TextAlignment.Center,
                width = 50,
                minWidth = 50,
                maxWidth = 50,
                autoResize = true,
                allowToggleVisibility = true
            };

            columns[3] = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent(TextContent.color),
                headerTextAlignment = TextAlignment.Center,
                width = 50,
                minWidth = 50,
                maxWidth = 50,
                autoResize = true,
                allowToggleVisibility = true
            };

            MultiColumnHeaderState multiColumnHeaderState = new MultiColumnHeaderState(columns);
            return new VisibilityToolColumnHeader(multiColumnHeaderState)
            {
                GetAllVisibility = GetAllVisibility,
                SetAllVisibility = SetAllVisibility,
                canSort = false,
                height = 20,
                visibilityColumn = 0
            };
        }
    }
}
