using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace G5.Logic
{
    public class DecisionMakingContext : IDisposable
    {
        private IntPtr _gc;

        public IntPtr GC
        {
            get
            {
                Debug.Assert(_gc != IntPtr.Zero);
                return _gc;
            }
        }

        public DecisionMakingContext()
        {
            _gc = DecisionMakingDll.CreateGameContext();
        }

        public void Dispose()
        {
            if (_gc != IntPtr.Zero)
            {
                DecisionMakingDll.ReleaseGameContext(_gc);
                _gc = IntPtr.Zero;
            }
        }
    }
}
