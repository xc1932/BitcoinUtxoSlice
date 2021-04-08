using System;
using System.Collections.Generic;
using System.Text;

namespace OrderedBlockchainParser
{
    public class FileStatusItem_Class
    {
        public string fileName;
        public int totalBlocks;
        public int unusedBlocks;
        public List<string> unusedBlockHash = new List<string>();

        public FileStatusItem_Class() { }

        public FileStatusItem_Class(string fileName, int totalBlocks, int unusedBlocks, List<string> unusedBlockHash)
        {
            this.fileName = fileName;
            this.totalBlocks = totalBlocks;
            this.unusedBlocks = unusedBlocks;
            this.unusedBlockHash = unusedBlockHash;
        }
    }
}
