using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using WinRT;

namespace HideMyWindows.App.Helpers
{
    public static class D3DCaptureHelper
    {
        [ComImport]
        [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IGraphicsCaptureItemInterop
        {
            [PreserveSig]
            int CreateForWindow(
                [In] IntPtr window,
                [In] ref Guid iid,
                out IntPtr result);

            [PreserveSig]
            int CreateForMonitor(
                [In] IntPtr monitor,
                [In] ref Guid iid,
                out IntPtr result);
        }

        public static GraphicsCaptureItem CreateItemForMonitor(IntPtr hmon)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CaptureHelper] Creating item for monitor {hmon}");
                var iid = typeof(IGraphicsCaptureItemInterop).GUID;
                var interop = GetActivationFactory<IGraphicsCaptureItemInterop>("Windows.Graphics.Capture.GraphicsCaptureItem", iid);
                
                System.Diagnostics.Debug.WriteLine($"[CaptureHelper] Got interop factory. Calling CreateForMonitor...");
                
                // IGraphicsCaptureItem IID
                var itemIID = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760"); 
                
                int hr = interop.CreateForMonitor(hmon, ref itemIID, out IntPtr itemPointer);
                
                System.Diagnostics.Debug.WriteLine($"[CaptureHelper] CreateForMonitor HRESULT: {hr} (0x{hr:X})");

                if (hr != 0 || itemPointer == IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine($"[CaptureHelper] Failed to create item. hr={hr}, ptr={itemPointer}");
                    return null!;
                }

                System.Diagnostics.Debug.WriteLine($"[CaptureHelper] Marshalling Item Ptr: {itemPointer}");
                var item = MarshalInterface<GraphicsCaptureItem>.FromAbi(itemPointer);
                return item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CaptureHelper] CreateItemForMonitor Exception: {ex}");
                throw;
            }
        }

        public static GraphicsCaptureItem CreateItemForWindow(IntPtr hwnd)
        {
             try
             {
                var iid = typeof(IGraphicsCaptureItemInterop).GUID;
                var interop = GetActivationFactory<IGraphicsCaptureItemInterop>("Windows.Graphics.Capture.GraphicsCaptureItem", iid);
                var itemIID = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760"); 
                var hr = interop.CreateForWindow(hwnd, ref itemIID, out var itemPointer);
                if (hr != 0 || itemPointer == IntPtr.Zero) return null!;
                var item = MarshalInterface<GraphicsCaptureItem>.FromAbi(itemPointer);
                return item;
             }
             catch (Exception ex)
             {
                 System.Diagnostics.Debug.WriteLine($"[CaptureHelper] CreateItemForWindow Exception: {ex}");
                 throw;
             }
        }

        [DllImport("combase.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int WindowsCreateString(
            [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
            int length,
            out IntPtr hstring);

        [DllImport("combase.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int WindowsDeleteString(IntPtr hstring);

        [DllImport("combase.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int RoGetActivationFactory(
            IntPtr activatableClassId,
            [In] ref Guid iid,
            out IntPtr factory);

        private static T GetActivationFactory<T>(string activatableClassId, Guid iid)
        {
            IntPtr hstring = IntPtr.Zero;
            try
            {
                if (WindowsCreateString(activatableClassId, activatableClassId.Length, out hstring) != 0)
                {
                    throw new Exception("Failed to create HString for " + activatableClassId);
                }

                if (RoGetActivationFactory(hstring, ref iid, out var factoryPtr) != 0)
                {
                    throw new Exception("Failed to get activation factory for " + activatableClassId);
                }

                try
                {
                    System.Diagnostics.Debug.WriteLine($"[CaptureHelper] RoGetActivationFactory success for {activatableClassId}. FactoryPtr: {factoryPtr}");
                    return (T)Marshal.GetObjectForIUnknown(factoryPtr);
                }
                finally
                {
                    Marshal.Release(factoryPtr);
                }
            }
            finally
            {
                if (hstring != IntPtr.Zero)
                {
                    WindowsDeleteString(hstring);
                }
            }
        }
        [ComImport]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDirect3DDxgiInterfaceAccess
        {
            IntPtr GetInterface([In] ref Guid iid);
        }

        [ComImport]
        [Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ID3D11Device
        {
           void CreateBuffer(); // Placeholder
           void CreateTexture1D(); // Placeholder
           void CreateTexture2D(
               [In] ref D3D11_TEXTURE2D_DESC pDesc,
               [In] IntPtr pInitialData,
               [Out] out IntPtr ppTexture2D);
           void CreateTexture3D(); // Placeholder
           void CreateShaderResourceView(); // Placeholder
           void CreateUnorderedAccessView(); // Placeholder
           void CreateRenderTargetView(); // Placeholder
           void CreateDepthStencilView(); // Placeholder
           void CreateInputLayout(); // Placeholder
           void CreateVertexShader(); // Placeholder
           void CreateGeometryShader(); // Placeholder
           void CreateGeometryShaderWithStreamOutput(); // Placeholder
           void CreatePixelShader(); // Placeholder
           void CreateHullShader(); // Placeholder
           void CreateDomainShader(); // Placeholder
           void CreateComputeShader(); // Placeholder
           void CreateClassLinkage(); // Placeholder
           void CreateBlendState(); // Placeholder
           void CreateDepthStencilState(); // Placeholder
           void CreateRasterizerState(); // Placeholder
           void CreateSamplerState(); // Placeholder
           void CreateQuery(); // Placeholder
           void CreatePredicate(); // Placeholder
           void CreateCounter(); // Placeholder
           void CreateDeferredContext(); // Placeholder
           void OpenSharedResource(); // Placeholder
           void CheckFormatSupport(); // Placeholder
           void CheckMultisampleQualityLevels(); // Placeholder
           void CheckCounterInfo(); // Placeholder
           void CheckCounter(); // Placeholder
           void CheckFeatureSupport(); // Placeholder
           void GetPrivateData(); // Placeholder
           void SetPrivateData(); // Placeholder
           void SetPrivateDataInterface(); // Placeholder
           void GetFeatureLevel(); // Placeholder
           void GetCreationFlags(); // Placeholder
           void GetDeviceRemovedReason(); // Placeholder
           void GetImmediateContext([Out] out ID3D11DeviceContext ppImmediateContext);
        }

        [ComImport]
        [Guid("c0bfa96c-e089-44fb-8eaf-26f8796190da")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ID3D11DeviceContext
        {
            // ID3D11DeviceChild methods
            void GetDevice([Out] out ID3D11Device ppDevice);
            void GetPrivateData([In] ref Guid guid, [In, Out] ref uint pDataSize, [Out] IntPtr pData);
            void SetPrivateData([In] ref Guid guid, [In] uint DataSize, [In] IntPtr pData);
            void SetPrivateDataInterface([In] ref Guid guid, [In] IntPtr pData);

            // ID3D11DeviceContext methods - VTABLE MUST MATCH D3D11 EXACTLY!
            // Slots 0-2 are IUnknown (QueryInterface, AddRef, Release)
            // Slots 3-6 are ID3D11DeviceChild (GetDevice, GetPrivateData, SetPrivateData, SetPrivateDataInterface)
            // ID3D11DeviceContext starts at slot 7:
            void VSSetConstantBuffers();                    // 7
            void PSSetShaderResources();                    // 8
            void PSSetShader();                             // 9
            void PSSetSamplers();                           // 10
            void VSSetShader();                             // 11
            void DrawIndexed();                             // 12
            void Draw();                                    // 13
            int Map(                                        // 14
                [In] IntPtr pResource,
                [In] uint Subresource,
                [In] uint MapType,
                [In] uint MapFlags,
                [Out] out D3D11_MAPPED_SUBRESOURCE pMappedResource);
            void Unmap(                                     // 15
                [In] IntPtr pResource,
                [In] uint Subresource);
            void PSSetConstantBuffers();                    // 16
            void IASetInputLayout();                        // 17
            void IASetVertexBuffers();                      // 18
            void IASetIndexBuffer();                        // 19
            void DrawIndexedInstanced();                    // 20
            void DrawInstanced();                           // 21
            void GSSetConstantBuffers();                    // 22
            void GSSetShader();                             // 23
            void IASetPrimitiveTopology();                  // 24
            void VSSetShaderResources();                    // 25
            void VSSetSamplers();                           // 26
            void Begin();                                   // 27
            void End();                                     // 28
            void GetData();                                 // 29
            void SetPredication();                          // 30
            void GSSetShaderResources();                    // 31
            void GSSetSamplers();                           // 32
            void OMSetRenderTargets();                      // 33
            void OMSetRenderTargetsAndUnorderedAccessViews(); // 34
            void OMSetBlendState();                         // 35
            void OMSetDepthStencilState();                  // 36
            void SOSetTargets();                            // 37
            void DrawAuto();                                // 38
            void DrawIndexedInstancedIndirect();            // 39
            void DrawInstancedIndirect();                   // 40
            void Dispatch();                                // 41
            void DispatchIndirect();                        // 42
            void RSSetState();                              // 43
            void RSSetViewports();                          // 44
            void RSSetScissorRects();                       // 45
            void CopySubresourceRegion();                   // 46
            void CopyResource([In] IntPtr pDstResource, [In] IntPtr pSrcResource); // 47
        }

        [ComImport]
        [Guid("dc8e63f3-d12b-4952-ba95-74f9a56a6a4d")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ID3D11Resource
        {
            void GetDevice();
            void GetPrivateData();
            void SetPrivateData();
            void SetPrivateDataInterface();
            void GetType();
            void SetEvictionPriority();
            void GetEvictionPriority();
        }

        [ComImport]
        [Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ID3D11Texture2D : ID3D11Resource
        {
            // ID3D11Resource methods
            new void GetDevice();
            new void GetPrivateData();
            new void SetPrivateData();
            new void SetPrivateDataInterface();
            new void GetType();
            new void SetEvictionPriority();
            new void GetEvictionPriority();

            void GetDesc([Out] out D3D11_TEXTURE2D_DESC pDesc);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D3D11_TEXTURE2D_DESC
        {
            public uint Width;
            public uint Height;
            public uint MipLevels;
            public uint ArraySize;
            public uint Format; // DXGI_FORMAT
            public uint SampleDesc_Count;
            public uint SampleDesc_Quality;
            public uint Usage;
            public uint BindFlags;
            public uint CPUAccessFlags;
            public uint MiscFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D3D11_MAPPED_SUBRESOURCE
        {
            public IntPtr pData;
            public uint RowPitch;
            public uint DepthPitch;
        }

        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);


        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int D3D11CreateDevice(
            IntPtr pAdapter,
            int DriverType,
            IntPtr Software,
            int Flags,
            IntPtr pFeatureLevels,
            int FeatureLevels,
            int SDKVersion,
            out IntPtr ppDevice,
            out int pFeatureLevel,
            out IntPtr ppImmediateContext);
    }
}
