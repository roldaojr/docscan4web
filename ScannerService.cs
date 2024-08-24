using NAPS2.Images.Wpf;
using NAPS2.Scan;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AustinHarris.JsonRpc;
using System.IO;
using System.Text.Json.Serialization;
using NAPS2.Images;

namespace docscan4web
{
    internal class ScannerService
    {
        private ScanController controller;
        private List<ScanDevice> availableDevices;

        public ScannerService()
        {
            using var scanningContext = new ScanningContext(new WpfImageContext());
            controller = new ScanController(scanningContext);
            availableDevices = [];
        }

        [JsonRpcMethod]
        public async Task<List<ScanDevice>> GetDeviceList()
        {
            availableDevices = await controller.GetDeviceList();
            return availableDevices;
        }

        [JsonRpcMethod]
        public async Task<ScanCaps> GetScanCaps(ScanDevice device)
        {
            return await controller.GetCaps(device);
        }

        [JsonRpcMethod]
        public async Task Scan(ScanOptions options)
        {
            controller.Scan(options);
        }
    }
}
