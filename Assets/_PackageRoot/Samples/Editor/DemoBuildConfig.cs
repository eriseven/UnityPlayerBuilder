using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Options;
using ProjectBuilder.Editor;
using UnityEngine;

[Serializable]
public class DemoBuildConfig : BuildConfig
{
    public string channel = "xxxxxxxxx";

    protected override (string, string, Action<string>)[] AdditionalOptions => new (string, string, Action<string>)[]
    {
        ("d|def=", "", v =>
        {
            if (!string.IsNullOrEmpty(v))
            {
            }
        }),
    };
}