using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainSceneInstaller : LifetimeScope
{
    [SerializeField] private WallGenerator _wallGenerator;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private MapEditorManager _mapEditorManager;
    [SerializeField] private MapObjectManager _mapObjectManager;
    
    [SerializeField] private ProtocolManager _protocolManager;
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(_uiManager);
        builder.RegisterComponent(_wallGenerator);
        builder.RegisterComponent(_mapEditorManager);
        builder.RegisterComponent(_mapObjectManager);

        builder.RegisterComponent(_protocolManager).As<IProtocolManager>();
        
        builder.Register<RestFulAPI>(Lifetime.Scoped);
        
        builder.RegisterEntryPoint<DataPresenter>(Lifetime.Scoped).As<IDataPresenterHandler>();
        builder.RegisterEntryPoint<MainPresenter>(Lifetime.Scoped);
        
        builder.Register<MapObjectHistoryController>(Lifetime.Scoped);
    }
}
