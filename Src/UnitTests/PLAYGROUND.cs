﻿//using System;

//class Foo
//{
//    void Bar()
//    {
//        Func<string, int> valid = _ => 42;
//        Func<string, int> invalid = _ => _.Length;

//        Func<string, string, string, int> valid2 = (_, __, ___, ____) => 42;
//        Func<string, string, string, int> invalid2 = (a, b, c, _) => _.Length;

//        Func<string, int> invalid3 = _ =>
//        {
//            if (DateTime.Now.Year == 2017)
//            {
//                return _.Length;
//            }

//            return 32;
//        };
//    }
//}