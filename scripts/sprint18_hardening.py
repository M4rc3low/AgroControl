from pathlib import Path

program = Path('src/backend/AgroControl.Api/Program.cs')
s = program.read_text()
if 'using AgroControl.Application.Telemetry;' not in s:
    s = s.replace('using AgroControl.Application.Sustainability;\n', 'using AgroControl.Application.Sustainability;\nusing AgroControl.Application.Telemetry;\n', 1)
if 'builder.Services.AddScoped<TelemetryAccessService>();' not in s:
    s = s.replace('builder.Services.AddScoped<RasterProcessingService>();\n', 'builder.Services.AddScoped<RasterProcessingService>();\nbuilder.Services.AddScoped<TelemetryAccessService>();\n', 1)
program.write_text(s)

endpoints = Path('src/backend/AgroControl.Api/Endpoints/TelemetryEndpoints.cs')
s = endpoints.read_text()
s = s.replace('ITelemetryClient client', 'TelemetryAccessService service')
s = s.replace('client.ListDevicesAsync', 'service.ListDevicesAsync')
s = s.replace('client.CreateDeviceAsync', 'service.CreateDeviceAsync')
s = s.replace('client.GetDeviceAsync', 'service.GetDeviceAsync')
s = s.replace('client.UpdateStatusAsync', 'service.UpdateStatusAsync')
s = s.replace('client.IngestAsync', 'service.IngestAsync')
s = s.replace('client.GetLatestAsync', 'service.GetLatestAsync')
s = s.replace('client.GetHistoryAsync', 'service.GetHistoryAsync')
endpoints.write_text(s)
