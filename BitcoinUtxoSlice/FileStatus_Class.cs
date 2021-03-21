using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinUtxoSlice
{
    class FileStatus_Class
    {
        public string fileName;
        public int totalBlocks;
        public int unusedBlocks;
        public bool readStatus = false;
        public int[] blockStatusArray;

        public FileStatus_Class() { 
        }

        public FileStatus_Class(string fileName,int totalBlocks) {
            this.fileName = fileName;
            this.totalBlocks = totalBlocks;
            this.unusedBlocks = totalBlocks;
            this.readStatus = true;
            this.blockStatusArray = new int[this.totalBlocks];
            
        }
    }
}
