using System.Collections;
using System.Collections.Generic;
using LunaLyrics.Data;
using UnityEngine;
using Zenject;

public class SignalInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        SignalBusInstaller.Install(Container);

        Container.DeclareSignal<UpdateMediaSignal>();
        Container.DeclareSignal<CheckNextLyricsSignal>();
        Container.DeclareSignal<NewLyricsSignal>();
    }
}
