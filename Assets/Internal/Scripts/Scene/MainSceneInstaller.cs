using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainSceneInstaller : LifetimeScope
{
    [SerializeField] private WallGenerator _wallGenerator;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private MapEditorManager _mapEditorManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(_uiManager);
        builder.RegisterComponent(_wallGenerator);
        builder.RegisterComponent(_mapEditorManager);
        builder.RegisterEntryPoint<MainPresenter>(Lifetime.Scoped);
    }
}
