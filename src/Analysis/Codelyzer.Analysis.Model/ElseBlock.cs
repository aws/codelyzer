using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Codelyzer.Analysis.Model
{
    public class ElseBlock : UstNode
    {

        public ElseBlock()
            : base(IdConstants.ElseBlockName)
        {
           
        }
    }
}