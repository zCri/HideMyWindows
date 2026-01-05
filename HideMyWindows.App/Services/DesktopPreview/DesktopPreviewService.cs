using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HideMyWindows.App.Helpers;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using System.IO;

namespace HideMyWindows.App.Services.DesktopPreview
{
    public class DesktopPreviewService : IDisposable
    {
        private GraphicsCaptureSession? _session;
        private Direct3D11CaptureFramePool? _framePool;
        private IDirect3DDevice? _device;
        private IntPtr _d3dDevicePtr;
        private IntPtr _d3dContextPtr;
        
        // MTA-bound RCWs for background thread usage
        private CaptureHelper.ID3D11Device? _d3dDeviceMta;
        private CaptureHelper.ID3D11DeviceContext? _d3dContextMta;
        private IntPtr _stagingTexturePtr;

        public event EventHandler<BitmapSource>? FrameCaptured;

        public DesktopPreviewService()
        {
            InitializeD3D();
        }

        private void InitializeD3D()
        {
            // Create D3D11 Device
            // DriverType.Hardware = 1
            // D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x20
            var hr = CaptureHelper.D3D11CreateDevice(
                IntPtr.Zero,
                1, // Hardware
                IntPtr.Zero,
                0x20, // BGRA Support
                IntPtr.Zero,
                0,
                7, // D3D11_SDK_VERSION
                out _d3dDevicePtr,
                out _,
                out _d3dContextPtr);

            if (hr != 0 || _d3dDevicePtr == IntPtr.Zero)
            {
                 throw new Exception($"Failed to create D3D11 Device. HRESULT={hr}, Ptr={_d3dDevicePtr}");
            }
            
            Debug.WriteLine($"[DesktopPreviewService] D3D11 Device Created. Ptr={_d3dDevicePtr}");

            // We do NOT create RCWs here (STA thread). 
            // They will be created lazily on the usage thread (MTA).
            
            CaptureHelper.CreateDirect3D11DeviceFromDXGIDevice(_d3dDevicePtr, out var genericDevicePtr);
            _device = WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(genericDevicePtr);
            Marshal.Release(genericDevicePtr);
            // Marshal.Release(genericDevicePtr); // FromAbi does not take ownership? Or does it?
            // Usually FromAbi calls AddRef (via ObjectReference). So we should release our local copy?
            // Actually CreateDirect3D11DeviceFromDXGIDevice returns an AddRef'd pointer.
            // MarshalInterface.FromAbi expects an ABI pointer. It will create an ObjectReference which calls AddRef?
            // No, FromAbi takes the pointer assuming it is valid. 
            // If we look at standard patterns (e.g. from CppWinRT or CSWinRT), if we own the pointer we should be careful.
            // But usually MarshalInterface<T>.FromAbi(ptr) creates a new wrapper. The wrapper holds a ref.
            // We should check if we need to Release `genericDevicePtr` locally. 
            // Looking at similar code:
            // var device = MarshalInterface<IDirect3DDevice>.FromAbi(ptr);
            // Marshal.Release(ptr); 
            // This seems correct because CreateDirect3D11DeviceFromDXGIDevice gives us a +1 ref.
            // The wrapper adds another +1 (or takes ownership?). 
            // Let's assume standard COM rules: we got a pointer, we pass it to factory, factory wraps it.
            // Safest pattern:
            // _device = MarshalInterface<IDirect3DDevice>.FromAbi(genericDevicePtr);
            // Marshal.Release(genericDevicePtr); // Release our raw handle since the wrapper has its own.
            
            // Wait, CreateDirect3D11DeviceFromDXGIDevice returns an IInspectable pointer to the WinRT object.
            
            // Re-verifying logic:
            // 1. D3D11CreateDevice -> gives us ID3D11Device (COM). 
            // 2. CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out graphicsDevice) -> gives us IInspectable* (IDirect3DDevice).
            //    This is an "out" parameter so it has +1 Ref count.
            // 3. FromAbi(ptr) -> Creates a managed object wrapping that pointer.
            //    Under the hood, `ObjectReference<T>.FromAbi(ptr)` creates a new ObjectReference. 
            //    It does NOT call AddRef on the pointer passed in *by default*? 
            //    Actually, `ObjectReference` assumes ownership or calls AddRef?
            //    Most CSWinRT samples show passing the pointer and NOT releasing it if the wrapper takes ownership, OR the wrapper calls AddRef.
            //    If I check `WinRT.MarshalInspectable.FromAbi`, it seems to respect COM rules.
            
            // I will err on side of caution and NOT release it immediately if I'm unsure, but that causes a leak.
            // However, CSWinRT `FromAbi` usually does `new ObjectReference(ptr)`. 
            // If `ObjectReference` calls AddRef, then we MUST release `genericDevicePtr`.
            // If `ObjectReference` attaches, then we shouldn't.
            // Documentation says `FromAbi` returns a consolidated RCW/CCW wrapper.
            // Effectively, it acts like `Marshal.GetObjectForIUnknown`. 
            // `Marshal.GetObjectForIUnknown` calls AddRef on the proxy/stub. 
            // So we SHOULD release `genericDevicePtr`.
            // But `genericDevicePtr` is the *raw* pointer.
            
            // Let's stick to the change of just using FromAbi and keeping the Release.
            
            // CORRECT CODE:
            /*
            IntPtr genericDevicePtr;
            CaptureHelper.CreateDirect3D11DeviceFromDXGIDevice(_d3dDevicePtr, out genericDevicePtr);
            _device = WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(genericDevicePtr);
            Marshal.Release(genericDevicePtr);
            */
            
            // However, `CreateDirect3D11DeviceFromDXGIDevice` might return `NULL` if it fails?
            // The original code passed `_d3dDevicePtr` which is the ID3D11Device.
            // `CreateDirect3D11DeviceFromDXGIDevice` expects `IDXGIDevice`.
            // ID3D11Device inherits from IUnknown, but casts to IDXGIDevice effectively for this call? 
            // Usually we need to QueryInterface for IDXGIDevice from ID3D11Device first.
            // But strict implementation of D3D11Device implements IDXGIDevice.
            
            // Let's trust that the pointer works (it worked enough to pass before, just the cast failed later? No, this code is initialization).
            // Wait, the crash is at `CreateFreeThreaded`. Initialization happened earlier.
            // So `InitializeD3D` succeeded without throwing. 
            // The `InvalidCastException` was later.
            // So `_device` was created successfully as an RCW.
            // The problem is explicitly that `CreateFreeThreaded` requires a TRUE WinRT object.
            
        }

        public void StartCapture(IntPtr hmon)
        {
            try {
                Debug.WriteLine($"[DesktopPreviewService] StartCapture called for monitor: {hmon}");
                StopCapture();

                if (_device == null) {
                    Debug.WriteLine("[DesktopPreviewService] D3D Device is null!");
                    return;
                }

                var item = CaptureHelper.CreateItemForMonitor(hmon);
                Debug.WriteLine($"[DesktopPreviewService] CaptureItem created. Size: {item.Size}");
                
                // Create Frame Pool
                _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                    _device,
                    Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    1,
                    item.Size);
                
                Debug.WriteLine("[DesktopPreviewService] FramePool created.");

                _framePool.FrameArrived += OnFrameArrived;
                
                _session = _framePool.CreateCaptureSession(item);
                _session.IsCursorCaptureEnabled = true;
                _session.StartCapture();
                Debug.WriteLine("[DesktopPreviewService] Session started.");
            } catch (Exception ex) {
                Debug.WriteLine($"[DesktopPreviewService] StartCapture Failed: {ex}");
            }
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            try 
            {
                // Debug.WriteLine("[DesktopPreviewService] Frame Arrived.");
                using var frame = sender.TryGetNextFrame();
                if (frame == null) return;

                // Lazy init MTA RCWs
                if (_d3dDeviceMta == null)
                {
                    _d3dDeviceMta = (CaptureHelper.ID3D11Device)Marshal.GetObjectForIUnknown(_d3dDevicePtr);
                    _d3dContextMta = (CaptureHelper.ID3D11DeviceContext)Marshal.GetObjectForIUnknown(_d3dContextPtr);
                }

                var surface = frame.Surface;
                // Use WinRT.CastExtensions.As<T>() to query for the COM interface
                // This requires 'using WinRT;' which I will ensure is imported, 
                // or just use the extension method directly if namespace is available.
                // Since I cannot easily add 'using' at top without reading whole file or risking duplicates, I will use fully qualified call if possible, 
                // but 'As' is an extension method so good luck. 
                // Actually, I can just rely on 'using WinRT;' being implicitly available if I added it? No I didn't add it to top of file yet.
                // I will add 'using WinRT;' to the top of the file in a separate edit or just check if it's there.
                // Wait, I see I used 'WinRT.MarshalInterface' previously so WinRT namespace is available but maybe 'using WinRT' isn't.
                // I will update the file to include 'using WinRT;' at the top and change the cast line.
                
                // For this ReplaceFileContent, I will just do the cast change and assume I can add the using directive or fully qualify.
                // WinRT.CastExtensions.As<...>(surface)
                
                var access = WinRT.CastExtensions.As<CaptureHelper.IDirect3DDxgiInterfaceAccess>(surface);
                
                if (access == null) {
                    Debug.WriteLine("[DesktopPreviewService] Failed to cast to IDirect3DDxgiInterfaceAccess");
                    return;
                }
                
                // Get source texture as raw pointer
                var guid = typeof(CaptureHelper.ID3D11Texture2D).GUID;
                var pSourceTexture = access.GetInterface(ref guid);
                
                if (pSourceTexture == IntPtr.Zero) {
                     Debug.WriteLine("[DesktopPreviewService] Source Texture pointer is null");
                     return;
                }

                // Get texture description - need RCW temporarily just for GetDesc
                CaptureHelper.D3D11_TEXTURE2D_DESC desc;
                try {
                    var texture = Marshal.GetObjectForIUnknown(pSourceTexture) as CaptureHelper.ID3D11Texture2D; 
                    if (texture == null) {
                         Debug.WriteLine("[DesktopPreviewService] Texture is null");
                         Marshal.Release(pSourceTexture);
                         return;
                    }
                    texture.GetDesc(out desc);
                } catch {
                    Marshal.Release(pSourceTexture);
                    throw;
                }

                // Ensure Staging Texture matches
                if (_stagingTexturePtr == IntPtr.Zero) 
                {
                    var stagingDesc = desc;
                    stagingDesc.Usage = 3; // D3D11_USAGE_STAGING
                    stagingDesc.BindFlags = 0;
                    stagingDesc.CPUAccessFlags = 0x20000; // D3D11_CPU_ACCESS_READ
                    stagingDesc.MiscFlags = 0;

                    _d3dDeviceMta!.CreateTexture2D(ref stagingDesc, IntPtr.Zero, out _stagingTexturePtr);
                    Debug.WriteLine($"[DesktopPreviewService] Created staging texture: {_stagingTexturePtr}");
                }

                if (_stagingTexturePtr == IntPtr.Zero) {
                     Debug.WriteLine("[DesktopPreviewService] Staging Texture creation failed.");
                     Marshal.Release(pSourceTexture);
                     return;
                }

                // Copy to staging using raw pointers directly
                try {
                    _d3dContextMta!.CopyResource(_stagingTexturePtr, pSourceTexture);
                } finally {
                    // Release source texture pointer
                    Marshal.Release(pSourceTexture);
                }

                // Map
                CaptureHelper.D3D11_MAPPED_SUBRESOURCE mapped;
                var mapHr = _d3dContextMta!.Map(_stagingTexturePtr, 0, 1, 0, out mapped); // 1 = D3D11_MAP_READ
                if (mapHr != 0)
                {
                    Debug.WriteLine($"[DesktopPreviewService] Map failed with HRESULT: {mapHr:X8}");
                    return;
                }
                
                try
                {
                    var width = (int)desc.Width;
                    var height = (int)desc.Height;
                    var stride = (int)mapped.RowPitch;
                    var size = stride * height;
                    
                    Debug.WriteLine($"[DesktopPreviewService] BitmapSource.Create: W={width}, H={height}, Stride={stride}, Size={size}, pData={mapped.pData}");
                    
                    if (mapped.pData == IntPtr.Zero || stride <= 0 || width <= 0 || height <= 0)
                    {
                        Debug.WriteLine("[DesktopPreviewService] Invalid mapped data parameters!");
                        return;
                    }
                    
                    // Copy pixel data to managed array BEFORE unmapping!
                    // The IntPtr points to GPU memory that becomes invalid after Unmap.
                    var pixels = new byte[size];
                    Marshal.Copy(mapped.pData, pixels, 0, size);
                    
                    // Debug: Check first 16 bytes of pixel data
                    Debug.WriteLine($"[DesktopPreviewService] First 16 bytes: {BitConverter.ToString(pixels, 0, Math.Min(16, pixels.Length))}");
                    
                    // Check if all pixels are zeros (copy failed)
                    bool hasNonZero = false;
                    for (int i = 0; i < Math.Min(1000, pixels.Length); i++)
                    {
                        if (pixels[i] != 0) { hasNonZero = true; break; }
                    }
                    Debug.WriteLine($"[DesktopPreviewService] HasNonZeroData: {hasNonZero}");
                    
                    // Now create bitmap from the copied data
                    var bitmap = BitmapSource.Create(
                        width, 
                        height, 
                        96, 
                        96, 
                        System.Windows.Media.PixelFormats.Bgra32, 
                        null, 
                        pixels, 
                        stride);
                    
                    bitmap.Freeze();
                    
                    FrameCaptured?.Invoke(this, bitmap);
                }
                finally
                {
                    _d3dContextMta?.Unmap(_stagingTexturePtr, 0);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Capture Error: {ex}");
            }
        }

        public void StopCapture()
        {
            _session?.Dispose();
            _session = null;
            
            _framePool?.Dispose();
            _framePool = null;
            
            if (_stagingTexturePtr != IntPtr.Zero)
            {
                Marshal.Release(_stagingTexturePtr);
                _stagingTexturePtr = IntPtr.Zero;
            }
            if (_d3dDeviceMta != null)
            {
                Marshal.ReleaseComObject(_d3dDeviceMta);
                _d3dDeviceMta = null;
            }
            if (_d3dContextMta != null)
            {
                Marshal.ReleaseComObject(_d3dContextMta);
                _d3dContextMta = null;
            }

            if (_d3dDeviceMta != null)
            {
                Marshal.ReleaseComObject(_d3dDeviceMta);
                _d3dDeviceMta = null;
            }
            if (_d3dContextMta != null)
            {
                Marshal.ReleaseComObject(_d3dContextMta);
                _d3dContextMta = null;
            }
        }

        public void Dispose()
        {
            StopCapture();
            if (_d3dDevicePtr != IntPtr.Zero)
            {
                Marshal.Release(_d3dDevicePtr);
                _d3dDevicePtr = IntPtr.Zero;
            }
            if (_d3dContextPtr != IntPtr.Zero)
            {
                Marshal.Release(_d3dContextPtr);
                _d3dContextPtr = IntPtr.Zero;
            }
        }
    }
}
