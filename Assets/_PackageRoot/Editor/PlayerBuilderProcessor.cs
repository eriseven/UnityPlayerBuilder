using Cysharp.Threading.Tasks;
using ProjectBuilder.Editor;
using UnityEditor.Build;

namespace PlayerBuilder.Eidtor
{
    public class PlayerBuilderProcessor : IOrderedCallback
    {
        public virtual async UniTask<int> Process(BuildConfig config)
        {
            return 0;
        }

        public virtual int callbackOrder { get; }

        public virtual bool AssetEdit => true;
    }
}