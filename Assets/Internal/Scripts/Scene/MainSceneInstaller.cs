using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainSceneInstaller : LifetimeScope
{
    [SerializeField] private WallGenerator _wallGenerator;
    [SerializeField] private UIManager _uiManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(_uiManager);
        builder.RegisterComponent(_wallGenerator);
        
        builder.RegisterEntryPoint<MainPresenter>(Lifetime.Scoped);
    }
}
