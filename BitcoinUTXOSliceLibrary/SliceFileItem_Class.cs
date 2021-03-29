using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BitcoinUTXOSliceLibrary
{
    public class SliceFileItem_Class
    {
        //Disposable
        // To detect redundant calls
        private bool _disposed = false;

        // Instantiate a SafeHandle instance.
        private SafeHandle _safeHandle = new SafeFileHandle(IntPtr.Zero, true);

        //基本属性
        public List<FileStatusItem_Class> fileStatusList = new List<FileStatusItem_Class>();
        public Dictionary<string, UTXOItem_Class> utxoDictionary = new Dictionary<string, UTXOItem_Class>();
        public List<string> forkedBlockList = new List<string>();//(正在修改.....)
        public List<string> orphanBlockList = new List<string>();//(正在修改.....)
        public string lastProcessedBlockHash;
        public int blockReadPointer;
        public int currentLoadFileNumber;
        public int processedBlockAmount;
        public int sliceFileAmount;
        public int sameTransactionCount;

        public SliceFileItem_Class() { }

        public SliceFileItem_Class(List<FileStatusItem_Class> fileStatusList, Dictionary<string, UTXOItem_Class> utxoDictionary,
            List<string> forkedBlockList, List<string> orphanBlockList, string lastProcessedBlockHash, int blockReadPointer,
            int currentLoadFileNumber, int processedBlockAmount, int sliceFileAmount, int sameTransactionCount)
        {
            this.fileStatusList = fileStatusList;
            this.utxoDictionary = utxoDictionary;
            this.forkedBlockList = forkedBlockList;//(正在修改.....)
            this.orphanBlockList = orphanBlockList;//(正在修改.....)
            this.lastProcessedBlockHash = lastProcessedBlockHash;
            this.blockReadPointer = blockReadPointer;
            this.currentLoadFileNumber = currentLoadFileNumber;
            this.processedBlockAmount = processedBlockAmount;
            this.sliceFileAmount = sliceFileAmount;
            this.sameTransactionCount = sameTransactionCount;
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
