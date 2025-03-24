using System;
using ProjectBuilder.Editor;
using UnityEditor.Build;

namespace PlayerBuilder.Eidtor
{
    public abstract class PlayerBuilderProcessor : IOrderedCallback
    {
        public abstract int Process(BuildConfig config);
        public virtual bool AssetEdit => true;
        public abstract bool IsDone();
        public string Error => this.Exception?.Message;
        public Exception Exception
        {
            get;
            protected set;
        } = null;

        public abstract int callbackOrder { get; }
    }
}