﻿using Utils.Document;

namespace Macro.Models
{
    public class ApplicationDataModel : IDocumentData
    {
        public string Code { get; set; }

        public bool IsDynamic { get; set; } = false;

        public string HandleName { get; set; }

        public string WheelHandleName { get; set; }

        public int OffsetX { get; set; }

        public int OffsetY { get; set; }
    }
}
