using RoR2.ContentManagement;
using System.Collections;

namespace ThrivingSurvivors;

public class ThrivingSurvivorsContent : IContentPackProvider
{
    public static ContentPack ContentPack { get; } = new();

    public string identifier => Plugin.GUID;

    public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
    {
        ContentPack.identifier = identifier;
        yield break;
    }

    public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
    {
        ContentPack.Copy(ContentPack, args.output);
        yield break;
    }

    public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
    {
        yield break;
    }
}