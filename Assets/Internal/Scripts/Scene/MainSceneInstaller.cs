using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainSceneInstaller : LifetimeScope
{
    [SerializeField] private WallGenerator _wallGenerator;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private MapEditorManager _mapEditorManager;
    [SerializeField] private MapObjectManager _mapObjectManager;
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(_uiManager);
        builder.RegisterComponent(_wallGenerator);
        builder.RegisterComponent(_mapEditorManager);
        builder.RegisterComponent(_mapObjectManager);
        builder.RegisterEntryPoint<MainPresenter>(Lifetime.Scoped);
    }
}
