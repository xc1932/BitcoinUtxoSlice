using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace OrderedBlockchainParser
{
    public class BlockProcessContextModel_Class
    {
        //Disposable
        // To detect redundant calls
        private bool _disposed = false;

        // Instantiate a SafeHandle instance.
        private SafeHandle _safeHandle = new SafeFileHandle(IntPtr.Zero, true);

        //基本属性
        public List<FileStatusItem_Class> fileStatusList = new List<FileStatusItem_Class>();
        public List<string> forkedBlockList = new List<string>();
        public List<string> orphanBlockList = new List<string>();
        public string lastProcessedBlockHash;
        public int processedBlockAmount = 0;
        public int blockReadPointer;
        public int currentLoadFileNumber;
        public DateTime blockProcessContextDatetime;

        public BlockProcessContextModel_Class() { }

        public BlockProcessContextModel_Class(List<FileStatusItem_Class> fileStatusList,List<string> forkedBlockList, 
            List<string> orphanBlockList,string lastProcessedBlockHash,int processedBlockAmount, int blockReadPointer,
            int currentLoadFileNumber, DateTime blockProcessContextDatetime)
        {
            this.fileStatusList = fileStatusList;
            this.forkedBlockList = forkedBlockList;
            this.orphanBlockList = orphanBlockList;
            this.lastProcessedBlockHash = lastProcessedBlockHash;
            this.processedBlockAmount = processedBlockAmount;
            this.blockReadPointer = blockReadPointer;
            this.currentLoadFileNumber = currentLoadFileNumber;
            this.blockProcessContextDatetime = blockProcessContextDatetime;
        }

        //Disposable Method
        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose() => Dispose(true);

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed state (managed objects).
                _safeHandle?.Dispose();
            }

            _disposed = true;
        }
    }
}
