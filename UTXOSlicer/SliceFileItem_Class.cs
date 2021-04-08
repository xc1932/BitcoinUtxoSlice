using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace UTXOSlicer
{
    public class SliceFileItem_Class
    {
        //Disposable
        // To detect redundant calls
        private bool _disposed = false;

        // Instantiate a SafeHandle instance.
        private SafeHandle _safeHandle = new SafeFileHandle(IntPtr.Zero, true);

        public Dictionary<string, UTXOItem_Class> utxoDictionary = new Dictionary<string, UTXOItem_Class>();
        public int sliceFileAmount;
        public int sameTransactionCount;

        public SliceFileItem_Class() { }

        public SliceFileItem_Class(Dictionary<string, UTXOItem_Class> utxoDictionary,int sliceFileAmount, int sameTransactionCount)
        {
            this.utxoDictionary = utxoDictionary;
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
