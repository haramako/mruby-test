using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MRuby
{
    public abstract class CSObject
    {
        //public abstract void CallMethod(MrbState mrb, Value sym);

        public static Value MethodFunc(MrbState mrb, Value _self)
        {
            //object self = (CSObject)_self.ToObject();
            return default;
        }
    }
}
