using System;
using System.Collections.Generic;
using System.Text;

namespace BlockchainTimeSliceProject
{
    class FileStatus_Class
    {
        string fileName { get; }
        int totalBlocks { get; }
        int unusedBlocks { get; set; }
        bool readStatus = false;
        int[] blockStatusArray { get; set; }

        public FileStatus_Class() { 
        }

        public FileStatus_Class(string fileName,int totalBlocks) {
            this.fileName = fileName;
            this.totalBlocks = totalBlocks;
            this.unusedBlocks = 0;
            this.readStatus = true;
            this.blockStatusArray = new int[this.totalBlocks];
        }
    }
}
