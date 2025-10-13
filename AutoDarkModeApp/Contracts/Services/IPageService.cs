namespace AutoDarkModeApp.Contracts.Services;

public interface IPageService
{
    Type GetPageType(string key);
    Type? GetPageParents(string key);
    List<Type> GetPageParentChain(string key);
}
