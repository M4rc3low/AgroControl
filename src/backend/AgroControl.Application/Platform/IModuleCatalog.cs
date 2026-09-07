namespace AgroControl.Application.Platform;

public interface IModuleCatalog
{
    IReadOnlyCollection<ModuleDefinition> GetAll();
}
