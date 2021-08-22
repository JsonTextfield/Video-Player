using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;

namespace Video_Player
{
    class RotationEffect : IBasicVideoEffect
    {
        private VideoEncodingProperties encodingProperties;

        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            this.encodingProperties = encodingProperties;
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            throw new NotImplementedException();
        }

        public void Close(MediaEffectClosedReason reason)
        {
            throw new NotImplementedException();
        }

        public void DiscardQueuedFrames()
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly => throw new NotImplementedException();

        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => throw new NotImplementedException();

        public MediaMemoryTypes SupportedMemoryTypes => throw new NotImplementedException();

        public bool TimeIndependent => throw new NotImplementedException();

        public void SetProperties(IPropertySet configuration)
        {
            throw new NotImplementedException();
        }
    }
}
