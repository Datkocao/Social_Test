using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Social_Test.Models;

namespace Social_Test.Controllers
{
    public class TesterController : Controller
    {
        private static readonly Regex IdRegex = new(
            "\"(?<k>[A-Za-z_]*[iI]d)\"\\s*:\\s*\"(?<v>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\"",
            RegexOptions.Compiled);

        private static readonly JsonSerializerOptions Pretty = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly IHttpClientFactory _factory;
        private readonly string _apiBase;
        private readonly string _googleClientId;

        public TesterController(IHttpClientFactory factory, IConfiguration cfg)
        {
            _factory = factory;
            _apiBase = (cfg["ApiBaseUrl"] ?? "http://localhost:5290").TrimEnd('/');
            _googleClientId = cfg["GoogleClientId"] ?? "";
        }

        // ---------- Trang chính ----------
        [HttpGet]
        public IActionResult Index(int? preset, string slot = "A")
        {
            var vm = NewVm(slot);
            if (preset is >= 0 && preset < Presets.All.Count)
            {
                var p = Presets.All[preset.Value];
                vm.Method = p.Method;
                vm.Path = p.Path;
                vm.Kind = p.Kind;
                vm.Body = p.Body;
                vm.FileField = p.FileField ?? "File";
            }
            return View(vm);
        }

        // ---------- Gửi request tuỳ ý ----------
        [HttpPost]
        public async Task<IActionResult> Send(string slot, string method, string path, string kind,
            string? body, string? fileField, bool noAuth, List<IFormFile>? files)
        {
            var vm = NewVm(slot);
            vm.Method = method; vm.Path = path; vm.Kind = kind; vm.Body = body;
            vm.FileField = string.IsNullOrWhiteSpace(fileField) ? "File" : fileField!;
            vm.NoAuth = noAuth;

            HttpContent? content = null;
            if (kind == "json")
            {
                content = new StringContent(string.IsNullOrWhiteSpace(body) ? "{}" : body!, Encoding.UTF8, "application/json");
            }
            else if (kind == "form")
            {
                var form = new MultipartFormDataContent();
                foreach (var line in (body ?? "").Split('\n'))
                {
                    var l = line.TrimEnd('\r');
                    var i = l.IndexOf('=');
                    if (i <= 0) continue;
                    form.Add(new StringContent(l[(i + 1)..], Encoding.UTF8), l[..i].Trim());
                }
                foreach (var f in files ?? new())
                {
                    if (f.Length == 0) continue;
                    var sc = new StreamContent(f.OpenReadStream());
                    if (!string.IsNullOrEmpty(f.ContentType))
                        sc.Headers.ContentType = MediaTypeHeaderValue.Parse(f.ContentType);
                    form.Add(sc, vm.FileField, f.FileName);
                }
                content = form;
            }

            vm.Result = await Call(new HttpMethod(method), path, content, noAuth ? null : Get(slot, "tok"));
            Remember(vm.Result.Body);
            vm.Ids = LoadIds();
            return View("Index", vm);
        }

        // ---------- Tài khoản test ----------
        [HttpPost]
        public Task<IActionResult> Login(string slot, string email, string password) =>
            AuthCall(slot, "/api/auth/login", new { email, password });

        [HttpPost]
        public Task<IActionResult> Register(string slot, string username, string email, string password,
            string confirmPassword, string fullName) =>
            AuthCall(slot, "/api/auth/register", new { username, email, password, confirmPassword, fullName });

        [HttpPost]
        public Task<IActionResult> Google(string slot, string idToken) =>
            AuthCall(slot, "/api/auth/google-login", new { idToken });

        [HttpPost]
        public Task<IActionResult> Refresh(string slot) =>
            AuthCall(slot, "/api/auth/refresh", new { refreshToken = Get(slot, "ref") ?? "" });

        [HttpPost]
        public IActionResult Logout(string slot)
        {
            foreach (var k in new[] { "tok", "ref", "who" }) HttpContext.Session.Remove($"{k}_{slot}");
            var vm = NewVm(slot);
            vm.Notice = $"Đã xoá token tài khoản {slot} (chưa revoke trên server).";
            return View("Index", vm);
        }

        [HttpPost]
        public IActionResult ClearIds()
        {
            HttpContext.Session.Remove("ids");
            return RedirectToAction(nameof(Index));
        }

        // ---------- Helpers ----------
        private async Task<IActionResult> AuthCall(string slot, string path, object payload)
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var vm = NewVm(slot);
            vm.Result = await Call(HttpMethod.Post, path, content, null);

            if (vm.Result.Status is >= 200 and < 300)
            {
                try
                {
                    using var doc = JsonDocument.Parse(vm.Result.Body);
                    var access = Find(doc.RootElement, "accessToken");
                    var refresh = Find(doc.RootElement, "refreshToken");
                    var mail = Find(doc.RootElement, "email");
                    if (!string.IsNullOrEmpty(access))
                    {
                        HttpContext.Session.SetString($"tok_{slot}", access);
                        if (!string.IsNullOrEmpty(refresh)) HttpContext.Session.SetString($"ref_{slot}", refresh);
                        HttpContext.Session.SetString($"who_{slot}", mail ?? "(không rõ email)");
                        vm.Notice = $"Đã lưu token cho tài khoản {slot}.";
                    }
                }
                catch (JsonException) { /* body không phải JSON */ }
            }

            Remember(vm.Result.Body);
            vm = Rebuild(vm);
            return View("Index", vm);
        }

        private TesterVm Rebuild(TesterVm vm)
        {
            vm.Who = new() { ["A"] = Get("A", "who"), ["B"] = Get("B", "who") };
            vm.Ids = LoadIds();
            return vm;
        }

        private TesterVm NewVm(string slot)
        {
            var vm = new TesterVm { Slot = slot == "B" ? "B" : "A", ApiBase = _apiBase, GoogleClientId = _googleClientId };
            return Rebuild(vm);
        }

        private string? Get(string slot, string key) => HttpContext.Session.GetString($"{key}_{slot}");

        private async Task<ResultVm> Call(HttpMethod method, string path, HttpContent? content, string? token)
        {
            path = path.Trim();
            if (!path.StartsWith('/')) path = "/" + path;

            var req = new HttpRequestMessage(method, _apiBase + path);
            if (content != null && method != HttpMethod.Get && method != HttpMethod.Head) req.Content = content;
            if (!string.IsNullOrEmpty(token)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var r = new ResultVm { Request = $"{method} {_apiBase}{path}" };
            var sw = Stopwatch.StartNew();
            try
            {
                var res = await _factory.CreateClient("api").SendAsync(req);
                r.Status = (int)res.StatusCode;
                r.Reason = res.ReasonPhrase ?? "";
                var mediaType = res.Content.Headers.ContentType?.MediaType ?? "";
                if (mediaType.StartsWith("image/") || mediaType.StartsWith("audio/") || mediaType.StartsWith("video/") ||
                    mediaType == "application/octet-stream")
                {
                    var bytes = await res.Content.ReadAsByteArrayAsync();
                    r.Body = $"[{mediaType}, {bytes.Length} bytes]";
                }
                else
                {
                    var text = await res.Content.ReadAsStringAsync();
                    try
                    {
                        using var doc = JsonDocument.Parse(text);
                        text = JsonSerializer.Serialize(doc.RootElement, Pretty);
                    }
                    catch (JsonException) { }
                    r.Body = text.Length > 200_000 ? text[..200_000] + "\n...(cắt bớt)" : text;
                }
            }
            catch (Exception ex)
            {
                r.Status = 0;
                r.Reason = "Không gọi được API";
                r.Body = ex.Message + "\n\nKiểm tra: API đã chạy chưa? ApiBaseUrl trong appsettings.json đúng cổng chưa?";
            }
            sw.Stop();
            r.Ms = sw.ElapsedMilliseconds;
            return r;
        }

        private static string? Find(JsonElement e, string name)
        {
            if (e.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in e.EnumerateObject())
                {
                    if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.String)
                        return p.Value.GetString();
                }
                foreach (var p in e.EnumerateObject())
                {
                    var found = Find(p.Value, name);
                    if (found != null) return found;
                }
            }
            else if (e.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in e.EnumerateArray())
                {
                    var found = Find(item, name);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private void Remember(string body)
        {
            var ids = LoadIds();
            var fresh = IdRegex.Matches(body).Select(m => $"{m.Groups["k"].Value} = {m.Groups["v"].Value}").Distinct().ToList();
            if (fresh.Count == 0) return;
            ids.RemoveAll(fresh.Contains);
            ids.InsertRange(0, fresh);
            if (ids.Count > 60) ids = ids.Take(60).ToList();
            HttpContext.Session.SetString("ids", JsonSerializer.Serialize(ids));
        }

        private List<string> LoadIds()
        {
            var s = HttpContext.Session.GetString("ids");
            return string.IsNullOrEmpty(s) ? new() : JsonSerializer.Deserialize<List<string>>(s) ?? new();
        }
    }
}
