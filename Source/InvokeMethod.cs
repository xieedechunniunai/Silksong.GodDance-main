// Reference: https://github.com/fifty-six/HollowKnight.Vasi/blob/master/Vasi/InvokeMethod.cs

using System;
using HutongGames.PlayMaker;

namespace GodDance.Source;

public class InvokeMethod : FsmStateAction {
    private readonly Action _action;

    public InvokeMethod(Action action) {
        _action = action;
    }

    public override void OnEnter() {
        _action.Invoke();

        Finish();
    }
}