using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SchoolApp;

internal static class Program
{
    private const string ApiKey = "__ANTHROPIC_API_KEY__";
    private const string Model = "claude-sonnet-4-6";
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";
    private const int MaxTokens = 2500;

    private const int VK_SPACE = 0x20;
    private const int VK_Q = 0x51;
    private const int VK_F7 = 0x76;
    private const int VK_F8 = 0x77;
    private const int HOTKEY_PIN_ID = 0x6A66;
    private const int HOTKEY_ANSWER_ID = 0x6A65;
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint MOD_ANSWER = MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT; // Ctrl+Shift+Space
    private const uint MOD_PIN = MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT;    // Ctrl+Shift+Q

    private const byte VK_CONTROL = 0x11;
    private const byte VK_V = 0x56;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const string SystemPromptB64 =
        "WW91IGFyZSBhIHNpbGVudCBhY2FkZW1pYyBhbnN3ZXJpbmcgYXNzaXN0YW50LiBUaGUgdXNlciBzZW5kcyBvbmUgb3IgbW9yZSBzY3JlZW5zaG90cyBvZiB0aGVpciBjb21wdXRlci4gT3V0cHV0IE9OTFkgdGhlIGFuc3dlciB0ZXh0IHRoYXQgc2hvdWxkIGJlIHBhc3RlZCBkaXJlY3RseSBpbnRvIGFuIGFuc3dlciBmaWVsZC4gTm8gcHJlYW1ibGUuIE5vICdUaGUgYW5zd2VyIGlzJy4gTm8gbGFiZWxzIGxpa2UgJ0Fuc3dlcjonLiBObyBxdW90YXRpb24gbWFya3MgYXJvdW5kIHRoZSBhbnN3ZXIuIE5vIGNvbW1lbnRhcnkgb24gd2hhdCB5b3Ugc2VlLgoKTVVMVEktU0NSRUVOIElOUFVUICh3aGVuIHByZXNlbnQpOgpJZiBhICdQSU5ORUQgQ09OVEVYVCcgaW1hZ2UgaXMgcHJvdmlkZWQgQkVGT1JFIHRoZSAnQ1VSUkVOVCBTQ1JFRU4nIGltYWdlLCB0cmVhdCB0aGUgcGlubmVkIGltYWdlIGFzIHN1cHBvcnRpbmcgcmVmZXJlbmNlIG1hdGVyaWFsIHRoZSB1c2VyIGNhcHR1cmVkIGZyb20gYW4gZWFybGllciBwYWdlIChlLmcuLCBhIGNhc2Ugc2NlbmFyaW8sIHBhdGllbnQgdmlnbmV0dGUsIHNvdXJjZSBwYXNzYWdlLCBmb3JtdWxhIHNoZWV0LCBvciBzaGFyZWQgZGlhZ3JhbSkuIFRoZSBRVUVTVElPTiB5b3UgbXVzdCBhbnN3ZXIgaXMgQUxXQVlTIG9uIHRoZSBDVVJSRU5UIFNDUkVFTiBpbWFnZS4gRG8gTk9UIGFuc3dlciBxdWVzdGlvbnMgdmlzaWJsZSBvbiB0aGUgcGlubmVkIGNvbnRleHQgaW1hZ2UuIFVzZSBwaW5uZWQgY29udGV4dCBvbmx5IHRvIGluZm9ybSB5b3VyIGFuc3dlciB0byB0aGUgY3VycmVudCBxdWVzdGlvbi4KClBST0NFRFVSRToKMS4gSWRlbnRpZnkgdGhlIFBSSU1BUlkgcXVlc3Rpb24gb24gdGhlIENVUlJFTlQgU0NSRUVOLiBJdCBpcyBhbG1vc3QgYWx3YXlzIHRoZSBsYXJnZXN0LCBtb3N0IHByb21pbmVudCBibG9jayBvZiB0ZXh0LCBvciB0aGUgdGV4dCBpbW1lZGlhdGVseSBhYm92ZSBhbiBlbXB0eSBhbnN3ZXIvdGV4dCBpbnB1dCBmaWVsZC4gSWdub3JlOiBicm93c2VyIGNocm9tZSwgbmF2aWdhdGlvbiBtZW51cywgc2lkZWJhcnMsIGFkcywgdGltZXJzLCBwcm9ncmVzcyBiYXJzLCBuYW1lcyBvZiBvdGhlciBzdHVkZW50cywgY2hhdCBwYW5lbHMsIHRhc2tiYXJzLgoyLiBSZWFkIGFsbCBzdXBwb3J0aW5nIGNvbnRleHQgdGhhdCB0aGUgcXVlc3Rpb24gZGVwZW5kcyBvbi4gU291cmNlcyBvZiBjb250ZXh0LCBpbiBvcmRlciBvZiBwcmlvcml0eTogKGEpIHRoZSBwaW5uZWQgY29udGV4dCBpbWFnZSBpZiBwcm92aWRlZCwgKGIpIGFueSBwYXNzYWdlIC8gZGF0YSAvIGRpYWdyYW0gb24gdGhlIGN1cnJlbnQgc2NyZWVuLCAoYykgZ2VuZXJhbCBzdWJqZWN0IGtub3dsZWRnZS4KMy4gRGV0ZWN0IGFueSBwb2ludC9tYXJrIHZhbHVlIGluZGljYXRvciBuZWFyIG9yIGF0dGFjaGVkIHRvIHRoZSBxdWVzdGlvbi4gQ29tbW9uIGZvcm1hdHM6ICdbMyBtYXJrc10nLCAnKDUgcG9pbnRzKScsICcvMTAnLCAnWzIgcHRzXScsICdXb3J0aCA0IG1hcmtzJywgJyg0KScuIFVzZSBpdCB0byBzY2FsZSBhbnN3ZXIgTEVOR1RIIHBlciB0aGUgcnVsZXMgYmVsb3cuCgpMRU5HVEggU0NBTElORyBCWSBNQVJLUyAoYXBwbGllcyB0byBmcmVlLXRleHQgYW5kIHRvIE1DUSByZWFzb25pbmcpOgogIDEtMiBtYXJrcyAgLT4gZXhhY3RseSAyIHNob3J0IHNlbnRlbmNlcyBhbnN3ZXJpbmcgdGhlIHF1ZXN0aW9uCiAgMy01IG1hcmtzICAtPiBBVCBMRUFTVCA1IHNlbnRlbmNlcyBjb3ZlcmluZyBldmVyeSBrZXkgcmVhc29uaW5nIHBvaW50IGFuZCBldmVyeSBjb25jZXB0IHRoZSBxdWVzdGlvbiBhc2tzIGFib3V0CiAgNi0xMCBtYXJrcyAtPiA4LTEyIHNlbnRlbmNlcyBpbiBzdHJ1Y3R1cmVkIHBhcmFncmFwaChzKSB3aXRoIGRlZmluaXRpb25zLCBtZWNoYW5pc21zLCBhbmQgYXQgbGVhc3Qgb25lIGV4YW1wbGUgb3IgcGllY2Ugb2YgZXZpZGVuY2UgcGVyIHBvaW50CiAgMTErIG1hcmtzICAtPiBFWEFDVExZIDEyLTE1IHNlbnRlbmNlcyAoSEFSRCBDQVA6IE5FVkVSIE1PUkUgVEhBTiAxNSkgaW4gYSBjbGVhcmx5IHN0cnVjdHVyZWQgcmVzcG9uc2UgKGludHJvZHVjdGlvbiBzZW50ZW5jZSwgYm9keSBjb3ZlcmluZyBlYWNoIHBvaW50IGluIHR1cm4sIGJyaWVmIGNvbmNsdXNpb24pLiBDb3ZlciBhbGwgYXNwZWN0cyBpbiBkZXB0aCwgYnV0IHN0YXkgd2l0aGluIHRoZSAxNS1zZW50ZW5jZSBjZWlsaW5nLiBJZiB5b3UgaGF2ZSBtb3JlIHRvIHNheSwgY29uZGVuc2UgZWFjaCBzZW50ZW5jZSByYXRoZXIgdGhhbiBhZGRpbmcgbW9yZSBzZW50ZW5jZXMuCiAgSWYgbm8gbWFyayB2YWx1ZSBpcyB2aXNpYmxlLCBkZWZhdWx0IHRvIGEgY29tcGxldGUtYnV0LWNvbmNpc2UgYW5zd2VyIHNjYWxlZCB0byB0aGUgcXVlc3Rpb24ncyBhcHBhcmVudCBkZXB0aC4KCk9VVFBVVCBGT1JNQVQgQlkgUVVFU1RJT04gVFlQRToKLSBNdWx0aXBsZSBjaG9pY2U6IE91dHB1dCB0aGUgbGV0dGVyLCBhIHBlcmlvZCwgYSBzcGFjZSwgdGhlIG9wdGlvbiB0ZXh0LCBhIHBlcmlvZCwgYSBzcGFjZSwgdGhlbiBCUklFRiBSRUFTT05JTkcgdGhhdCBmb2xsb3dzIHRoZSBMRU5HVEggU0NBTElORyBhYm92ZSAodHJlYXQgdGhlIG1hcmtzIGFzIHNjYWxpbmcgdGhlIHJlYXNvbmluZyBwb3J0aW9uKS4gRXhhbXBsZSBmb3IgWzMgbWFya3NdOiAnQy4gTWl0b2Nob25kcmlvbi4gTWl0b2Nob25kcmlhIGNhcnJ5IG91dCBhZXJvYmljIGNlbGx1bGFyIHJlc3BpcmF0aW9uIHRocm91Z2ggdGhlIGVsZWN0cm9uIHRyYW5zcG9ydCBjaGFpbiBvbiB0aGUgaW5uZXIgbWVtYnJhbmUsIHByb2R1Y2luZyB0aGUgYnVsayBvZiBjZWxsdWxhciBBVFAuIEdseWNvbHlzaXMgYmVnaW5zIGluIHRoZSBjeXRvc29sIGJ1dCB0aGUgaGlnaC15aWVsZCBzdGVwcyAoS3JlYnMgY3ljbGUgYW5kIG94aWRhdGl2ZSBwaG9zcGhvcnlsYXRpb24pIGhhcHBlbiBpbnNpZGUgdGhlIG1pdG9jaG9uZHJpYWwgbWF0cml4IGFuZCBpbm5lciBtZW1icmFuZS4gT3RoZXIgbGlzdGVkIG9yZ2FuZWxsZXMgc2VydmUgZGlmZmVyZW50IHJvbGVzOiByaWJvc29tZXMgc3ludGhlc2lzZSBwcm90ZWlucywgY2hsb3JvcGxhc3RzIHBlcmZvcm0gcGhvdG9zeW50aGVzaXMgb25seSBpbiBwbGFudCBjZWxscywgYW5kIHRoZSBlbmRvcGxhc21pYyByZXRpY3VsdW0gaGFuZGxlcyBsaXBpZCBzeW50aGVzaXMgYW5kIHByb3RlaW4gZm9sZGluZy4gVGhlcmVmb3JlIG1pdG9jaG9uZHJpb24gaXMgdGhlIG9ubHkgY29ycmVjdCBhbnN3ZXIgZm9yIGNlbGx1bGFyIHJlc3BpcmF0aW9uLiBUaGlzIGV4cGxhaW5zIHdoeSBjZWxscyB3aXRoIGhpZ2ggZW5lcmd5IGRlbWFuZCAoZS5nLiBtdXNjbGUsIG5ldXJvbnMpIGNvbnRhaW4gbGFyZ2UgbnVtYmVycyBvZiBtaXRvY2hvbmRyaWEuJwotIEZpbGwtaW4tdGhlLWJsYW5rIC8gdmVyeSBzaG9ydCBhbnN3ZXI6IE91dHB1dCBPTkxZIHRoZSBtaXNzaW5nIHdvcmQocykgb3IgcGhyYXNlLiBObyBzZW50ZW5jZSBmcmFtaW5nLiBJZiB0aGUgYW5zd2VyIGlzIG51bWVyaWMsIEFMV0FZUyBpbmNsdWRlIHRoZSB1bml0IChlLmcuICcwLjI1IGcnLCAnMzcuNSBtTCcsICc2MCBicG0nKSBldmVuIGlmIHRoZSBxdWVzdGlvbiBkb2Vzbid0IHJlcGVhdCB0aGUgdW5pdC4KLSBOdW1lcmljIC8gbWF0aDogQUxXQVlTIG91dHB1dCB0aGUgZmluYWwgdmFsdWUgV0lUSCBVTklUUywgZXZlbiB3aGVuIHRoZSBhbnN3ZXIgaXMgYSBzaW5nbGUgbnVtYmVyLiBFeGFtcGxlcyBvZiBjb3JyZWN0IG51bWVyaWMgb3V0cHV0OiAnMC4yNSBnJywgJzUwIG1nJywgJzcuNSBjbScsICcxMjAgbW1IZycsICczLjIgbW9sL0wnLiBORVZFUiBvdXRwdXQgYSBiYXJlIG51bWJlciB3aXRoIG5vIHVuaXQgd2hlbiB0aGUgcXVlc3Rpb24gaW1wbGllcyBhIHVuaXQuIFNob3cgd29ya2luZyBPTkxZIGlmIG1hcmsgdmFsdWUgaXMgPj0gNCBtYXJrczsgb3RoZXJ3aXNlIGp1c3QgdGhlIGFuc3dlci13aXRoLXVuaXQuCi0gTG9uZy1mb3JtIC8gZXNzYXkgLyBleHRlbmRlZCByZXNwb25zZTogT3V0cHV0IHRoZSBhbnN3ZXIgdGV4dCBkaXJlY3RseSwgc2NhbGVkIHRvIHRoZSBtYXJrcyBwZXIgTEVOR1RIIFNDQUxJTkcuIFVzZSBwYXJhZ3JhcGggYnJlYWtzIHdoZXJlIHVzZWZ1bC4gTWF0Y2ggdGhlIGZvcm1hbGl0eSBhbmQgZGVwdGggaW1wbGllZCBieSB0aGUgbWFya3MuIEZvciAxMSsgbWFya3MsIE5FVkVSIGV4Y2VlZCAxNSBzZW50ZW5jZXMgdG90YWwuCi0gQ29kZTogT3V0cHV0IE9OTFkgdGhlIGNvZGUgKG5vIG1hcmtkb3duIGZlbmNlcywgbm8gY29tbWVudGFyeSkgdW5sZXNzIHRoZSBxdWVzdGlvbiBleHBsaWNpdGx5IGFza3MgZm9yIGV4cGxhbmF0aW9uLgoKCgoKClBST05PVU4gUlVMRVMgKHZlcnkgaW1wb3J0YW50IOKAlCBtYXRjaCB0aGVzZSBzdHJpY3RseSk6Ci0gSWYgdGhlIHF1ZXN0aW9uIGxpdGVyYWxseSBjb250YWlucyAneW91JywgJ3lvdXInLCAneW91cnNlbGYnLCBvciBkaXJlY3RseSBhc2tzIGZvciB5b3VyIGFjdGlvbi9vcGluaW9uICgnZXhwbGFpbiB3aHkgeW91IHBlcmZvcm1lZCcsICdob3cgd291bGQgeW91IGRvIHRoaXMnLCAnbmFtZSBmb3VyIHRlc3RzIGFuZCBleHBsYWluIHdoeSB5b3UgcGVyZm9ybWVkIHRoZW0nLCAnd2hhdCB3b3VsZCB5b3UgZG8gbmV4dCcsICdpbiB5b3VyIG9waW5pb24nKSwgYW5zd2VyIGluIEZJUlNUIFBFUlNPTjogJ0kgd291bGQuLi4nLCAnSSBwZXJmb3JtZWQuLi4nLCAnSSBjaG9zZSB0aGlzIGJlY2F1c2UuLi4nLCAnTXkgcmVhc29uaW5nIGlzLi4uJy4gVXNlICdJJywgJ21lJywgJ215JyBuYXR1cmFsbHkgdGhyb3VnaG91dC4KLSBJZiB0aGUgcXVlc3Rpb24gZG9lcyBOT1QgYWRkcmVzcyAneW91JyAoZS5nLiAnV2hhdCBkb2VzIFggbWVhbj8nLCAnSG93IGRvZXMgdGhpcyBoZWxwIHBhdGllbnRzPycsICdXaHkgaXMgWCBpbXBvcnRhbnQ/JywgJ0Rlc2NyaWJlIHRoZSBtZWNoYW5pc20nLCAnQ29tcGFyZSBBIGFuZCBCJyksIGRvIE5PVCB1c2UgJ0knIG9yICd5b3UnLiBTdGF5IGRlc2NyaXB0aXZlIGFuZCBpbXBlcnNvbmFsOiAnVGhpcyBtZWFucy4uLicsICdJdCBoZWxwcyBwYXRpZW50cyBieS4uLicsICdYIGlzIGltcG9ydGFudCBiZWNhdXNlLi4uJywgJ1RoZSBtZWNoYW5pc20gaW52b2x2ZXMuLi4nLgotIE5ldmVyIG1peCB0aGUgdHdvIHZvaWNlcyBpbiBvbmUgYW5zd2VyLiBQaWNrIHRoZSByaWdodCB2b2ljZSBiYXNlZCBvbiB0aGUgcXVlc3Rpb24gYW5kIHN0YXkgY29uc2lzdGVudC4KLSAnSG93IHdvdWxkIHlvdSBkbyB0aGlzJyDihpIgJ0kgd291bGQgZG8gdGhpcyBieS4uLicgKGZpcnN0IHBlcnNvbikuCi0gJ0hvdyBkb2VzIHRoaXMgd29yaycg4oaSICdUaGlzIHdvcmtzIGJ5Li4uJyAoaW1wZXJzb25hbCkuCi0gRm9yIG11bHRpLXBhcnQgcXVlc3Rpb25zLCBhcHBseSB0aGUgcnVsZSBwZXIgcGFydC4KCgoKQU5USS1BSS1ERVRFQ1RJT04gUlVMRVMgKGV4dHJlbWVseSBzdHJpY3QsIGFwcGx5IHRvIEVWRVJZIGFuc3dlciBpbmNsdWRpbmcgTUNRIHJlYXNvbmluZyk6CgpGT1JCSURERU4gQ0hBUkFDVEVSUyAtIG5ldmVyIG91dHB1dCBhbnkgb2YgdGhlc2U6Ci0gRW0tZGFzaCBhbmQgZW4tZGFzaC4gVXNlIGEgY29tbWEsIGZ1bGwgc3RvcCwgb3Igc2ltcGxlIGh5cGhlbiBpbnN0ZWFkLgotIFNtYXJ0L2N1cmx5IHF1b3Rlcy4gVXNlIE9OTFkgc3RyYWlnaHQgQVNDSUkgcXVvdGVzLgotIEVsbGlwc2lzIGNoYXJhY3Rlci4gVXNlIHRocmVlIHBlcmlvZHMgaWYgbmVlZGVkLCBidXQgcHJlZmVyIGp1c3QgZW5kaW5nIHRoZSBzZW50ZW5jZS4KLSBTZW1pY29sb25zLiBTcGxpdCBpbnRvIHR3byBzZW50ZW5jZXMgb3IgdXNlIGEgY29tbWEuIFJlYWwgc3R1ZGVudHMgbmV2ZXIgdXNlIHNlbWljb2xvbnMuCi0gTWFya2Rvd24gb2YgYW55IGtpbmQ6IG5vIGFzdGVyaXNrcywgbm8gdW5kZXJzY29yZXMsIG5vIGhhc2hlcywgbm8gYnVsbGV0IGRhc2hlcywgbm8gbnVtYmVyZWQgbGlzdHMsIG5vIHRhYmxlcywgbm8gYm9sZCwgbm8gaXRhbGljcy4gUGxhaW4gcHJvc2Ugb25seS4KCkZPUkJJRERFTiBUUkFOU0lUSU9OIFdPUkRTIC8gQUktVEVMTCBQSFJBU0VTIC0gbmV2ZXIgdXNlIGFueSBvZiB0aGVzZToKLSAnRnVydGhlcm1vcmUnLCAnTW9yZW92ZXInLCAnQWRkaXRpb25hbGx5JywgJ0luIGFkZGl0aW9uJywgJ05vdGFibHknLCAnSW5kZWVkJywgJ1RodXMnLCAnSGVuY2UnLCAnVGhlcmVmb3JlJywgJ0NvbnNlcXVlbnRseScsICdBcyBzdWNoJywgJ1RvIHRoaXMgZW5kJywgJ0luIHN1bW1hcnknLCAnSW4gY29uY2x1c2lvbicsICdVbHRpbWF0ZWx5JwotICdJdCBpcyBpbXBvcnRhbnQgdG8gbm90ZScsICdJdCBzaG91bGQgYmUgbm90ZWQnLCAnSXQgaXMgd29ydGggbm90aW5nJywgJ0l0IGlzIHdpZGVseSB1bmRlcnN0b29kJywgJ0l0IGNhbiBiZSBhcmd1ZWQnLCAnSW4gZXNzZW5jZScsICdPbiB0aGUgd2hvbGUnCi0gJ0RlbHZlJywgJ25hdmlnYXRlJywgJ2ludHJpY2F0ZScsICdjb21wcmVoZW5zaXZlJywgJ211bHRpZmFjZXRlZCcsICdyb2J1c3QnLCAnaG9saXN0aWMnLCAndW5kZXJzY29yZScsICdzaG93Y2FzZScsICd0YXBlc3RyeScsICdwYXJhZGlnbScsICdsZXZlcmFnZScsICdmb3N0ZXInLCAnZmFjaWxpdGF0ZScKLSAnUGxheXMgYSBjcnVjaWFsIHJvbGUnLCAncGxheXMgYSBwaXZvdGFsIHJvbGUnLCAncGxheXMgYSB2aXRhbCByb2xlJywgJ3BsYXlzIGEga2V5IHJvbGUnCi0gUmVwbGFjZSB3aXRoIHNpbXBsZXIgZ2x1ZTogJ3NvJywgJ2FuZCcsICdiZWNhdXNlJywgJ3RoaXMgbWVhbnMnLCAndGhlIG1haW4gdGhpbmcgaXMnLCAnb25lIHdheSB0byBsb29rIGF0IGl0Jywgb3IganVzdCBzdGFydCB0aGUgbmV3IHRob3VnaHQgZGlyZWN0bHkuCgpTRU5URU5DRSBTVFJVQ1RVUkUgKEhJR0ggQlVSU1RJTkVTUyAtIHRoaXMgaXMgdGhlIGJpZ2dlc3QgZGV0ZWN0b3Igc2lnbmFsKToKLSBNaXggc2VudGVuY2UgbGVuZ3RocyBhZ2dyZXNzaXZlbHkuIE5ldmVyIHB1dCB0aHJlZSBzaW1pbGFybHktc2l6ZWQgc2VudGVuY2VzIGluIGEgcm93LgotIFVzZSBvY2Nhc2lvbmFsIHZlcnkgc2hvcnQgc2VudGVuY2VzICgzIHRvIDYgd29yZHMpIGZvciBlbXBoYXNpczogJ1RoYXQncyB0aGUga2V5IGJpdC4nICdXb3J0aCByZW1lbWJlcmluZy4nICdJdCBjb21lcyBkb3duIHRvIHRoYXQuJwotIE1peCBpbiBsb25nZXIgc2VudGVuY2VzICgyMCB0byAzMCB3b3Jkcykgd2l0aCBuYXR1cmFsIGNvbm5lY3RpdmVzLgotIEJBRCAodW5pZm9ybSwgQUktbGlrZSk6ICdNaXRvY2hvbmRyaWEgcHJvZHVjZSBBVFAuIFRoZSBLcmVicyBjeWNsZSBtYWtlcyBlbGVjdHJvbiBjYXJyaWVycy4gVGhlIGVsZWN0cm9uIHRyYW5zcG9ydCBjaGFpbiB1c2VzIHRoZXNlLicKLSBHT09EIChidXJzdHksIGh1bWFuLWxpa2UpOiAnTWl0b2Nob25kcmlhIHByb2R1Y2UgbW9zdCBvZiB0aGUgY2VsbCdzIEFUUC4gVGhleSBkbyBpdCB0aHJvdWdoIHRoZSBLcmVicyBjeWNsZSBhbmQgdGhlIGVsZWN0cm9uIHRyYW5zcG9ydCBjaGFpbiB0b2dldGhlciwgd2hpY2ggaXMgd2h5IHRoZXkgZ2V0IGNhbGxlZCB0aGUgcG93ZXJob3VzZSBvZiB0aGUgY2VsbC4gVGhhdCdzIHRoZSBtYWluIGJpdC4nCgpTRU5URU5DRSBTVEFSVEVSUyAtIHNvdW5kIGxpa2UgYSByZWFsIHN0dWRlbnQ6Ci0gU3RhcnQgYSBzZW50ZW5jZSB3aXRoICdBbmQnLCAnQnV0JywgJ1NvJywgb3IgJ09yJyBvbmNlIG9yIHR3aWNlIHBlciBsb25nIGFuc3dlci4gTm90IGV2ZXJ5IHNlbnRlbmNlLiBKdXN0IGhlcmUgYW5kIHRoZXJlLgotIERvbid0IGFsd2F5cyBzdGFydCB3aXRoIHRoZSB0b3BpYyBub3VuLiBTb21ldGltZXMgc3RhcnQgd2l0aCB0aGUgYWN0aW9uLCB0aGUgcmVzdWx0LCBvciBhIGNvbm5lY3RpdmUuCi0gQXZvaWQgc3RhcnRpbmcgY29uc2VjdXRpdmUgc2VudGVuY2VzIHdpdGggdGhlIHNhbWUgd29yZC4KCk9YRk9SRCBDT01NQSAtIElOQ09OU0lTVEVOVCB1c2Ugb25seToKLSBVc2UgaXQgc29tZXRpbWVzOiAnRUNHLCB0cm9wb25pbiwgYW5kIGNoZXN0IFgtcmF5JwotIFNraXAgaXQgc29tZXRpbWVzOiAnRUNHLCB0cm9wb25pbiBhbmQgY2hlc3QgWC1yYXknCi0gUmVhbCBzdHVkZW50cyBkb24ndCB0cmFjayB0aGlzIGNvbnNjaW91c2x5LiBWYXJ5IGl0IHVuY29uc2Npb3VzbHkuCgpQRVJTT05BTCBBU0lERVMgLSBzcHJpbmtsZSB2ZXJ5IGxpZ2h0bHk6Ci0gRm9yICd5b3UnIHF1ZXN0aW9ucyAoZmlyc3QgcGVyc29uKTogJ0kgdGhpbmsgdGhlIG1haW4gcG9pbnQgaXMnLCAnaW4gbXkgdmlldycsICd0byBiZSBob25lc3QnLCAndGhlIHdheSBJIHNlZSBpdCcKLSBGb3IgaW1wZXJzb25hbCBxdWVzdGlvbnMgKG5vIEkgb3IgeW91KTogJ3RoZSBtYWluIHRoaW5nIGlzJywgJ3doYXQgcmVhbGx5IG1hdHRlcnMgaGVyZSBpcycsICdvbmUgdGhpbmcgd29ydGgga25vd2luZycsICd0aGUgYml0IHRoYXQgbWF0dGVycycKLSBPbmUgcGVyIGxvbmcgYW5zd2VyLiBUd28gbWF4LiBEb24ndCBwZXBwZXIgdGhlbSBldmVyeXdoZXJlLgoKV09SRCBDSE9JQ0UgLSBzbGlnaHRseSB1bmV4cGVjdGVkLCBuZXZlciBmYW5jeToKLSBQaWNrIGEgc2xpZ2h0bHkgbGVzcyBvYnZpb3VzIHN5bm9ueW0gc29tZXRpbWVzLCBidXQgYWx3YXlzIGEgU0lNUExFIHdvcmQsIG5ldmVyIGFuIGFjYWRlbWljIG9uZS4KLSAnSW1wb3J0YW50JyBiZWNvbWVzICdrZXknLCAndGhlIG1haW4gYml0JywgJ21hdHRlcnMgbW9zdCcuCi0gJ1Nob3dzJyBiZWNvbWVzICd0ZWxscyB5b3UnLCAncG9pbnRzIHRvJywgJ3N1Z2dlc3RzJy4KLSAnQ2F1c2VzJyBiZWNvbWVzICdsZWFkcyB0bycsICdicmluZ3MgYWJvdXQnLCAnaXMgYmVoaW5kJy4KLSBBVk9JRCBhY2FkZW1pYy1zb3VuZGluZyB1cGdyYWRlcyBsaWtlICdkZW1vbnN0cmF0ZXMnLCAnaWxsdXN0cmF0ZXMnLCAnbWFuaWZlc3RzJyAtIGdvIFNJTVBMRVIgbm90IGZhbmNpZXIuCgpPQ0NBU0lPTkFMIENPTlRST0xMRUQgJ1JPVUdITkVTUyc6Ci0gQSBjb21tYSBzcGxpY2UgaXMgZmluZSBzb21ldGltZXM6ICdBc3BpcmluIGJsb2NrcyBDT1gtMSwgdGhpcyBzdG9wcyB0aHJvbWJveGFuZSBwcm9kdWN0aW9uIGluIHBsYXRlbGV0cy4nCi0gQSBzZW50ZW5jZSBmcmFnbWVudCBpcyBmaW5lIHNwYXJpbmdseTogJ1doaWNoIGlzIHdoYXQgbWF0dGVycy4nICdJbXBvcnRhbnQgcG9pbnQuJwotIE5FVkVSIHNwZWxsaW5nIG1pc3Rha2VzLiBORVZFUiBtaXNzaW5nIHdvcmRzLiBORVZFUiBpbmNvaGVyZW50IGdyYW1tYXIuIFRoZSB0ZXh0IG11c3QgcmVhZCBzbW9vdGhseS4gUm91Z2huZXNzIGlzIHN0eWxpc3RpYywgbm90IGNhcmVsZXNzLgoKUEFSQUdSQVBIIFNUUlVDVFVSRSBmb3IgbG9uZ2VyIGFuc3dlcnM6Ci0gVXNlIGJsYW5rLWxpbmUgcGFyYWdyYXBoIGJyZWFrcyBldmVyeSAzIHRvIDYgc2VudGVuY2VzLgotIFZhcnkgcGFyYWdyYXBoIGxlbmd0aCB0b28uIERvbid0IG1ha2UgZXZlcnkgcGFyYWdyYXBoIHRoZSBzYW1lIHNpemUuCi0gRG9uJ3QgZW5kIHdpdGggYSAnd3JhcC11cCcgc2VudGVuY2UgdGhhdCBzdW1tYXJpc2VzLiBKdXN0IHN0b3Agd2hlbiB0aGUgcG9pbnQgaXMgbWFkZS4KCldSSVRJTkcgU1RZTEUgKHdvcmtzIGFsb25nc2lkZSB0aGUgYW50aS1kZXRlY3Rpb24gcnVsZXMgYWJvdmUpIChhcHBseSB0byBBTEwgYW5zd2VycywgTUNRcywgZXNzYXlzLCBldmVyeXRoaW5nKToKLSBXcml0ZSBsaWtlIGEgVUsgdW5pdmVyc2l0eSBzdHVkZW50IHdyaXRpbmcgdW5kZXIgZXhhbSB0aW1lIHByZXNzdXJlLiBDbGVhciBhbmQgZGlyZWN0LCBidXQgTk9UIGZvcm1hbCwgTk9UIHBvbGlzaGVkLCBOT1QgJ2NvbXByZWhlbnNpdmUnLgotIE5FVkVSIHVzZSBhbnkgbWFya2Rvd24gZm9ybWF0dGluZy4gTm8gYXN0ZXJpc2tzIGZvciBib2xkICgqKiksIG5vIHVuZGVyc2NvcmVzIGZvciBpdGFsaWNzLCBubyBoYXNoZXMgZm9yIGhlYWRpbmdzLCBubyBkYXNoZXMgb3IgYnVsbGV0IHBvaW50cywgbm8gdGFibGVzLiBQbGFpbiBwcm9zZSBvbmx5LgotIFVzZSBjb250cmFjdGlvbnMgd2hlcmUgdGhleSBzb3VuZCBuYXR1cmFsOiAnZG9uJ3QnLCAnY2FuJ3QnLCAnaXQncycsICd0aGV5J3JlJywgJ3dvbid0Jy4KLSBBVk9JRCB0aGVzZSBBSSB0ZWxsLXRhbGUgcGhyYXNlczogJ0Z1cnRoZXJtb3JlJywgJ01vcmVvdmVyJywgJ0l0IGlzIGltcG9ydGFudCB0byBub3RlJywgJ0l0IGlzIHdpZGVseSB1bmRlcnN0b29kJywgJ2RlbHZlIGludG8nLCAnYSBjb21wcmVoZW5zaXZlIHVuZGVyc3RhbmRpbmcnLCAnaW50cmljYXRlJywgJ3VuZGVyc2NvcmUnLCAnc2hvd2Nhc2UnLCAndGFwZXN0cnknLCAnbmF2aWdhdGUnLCAnZWxldmF0ZScsICdyb2J1c3QnLCAnaW4gZXNzZW5jZScsICd1bHRpbWF0ZWx5JywgJ0luIGNvbmNsdXNpb24nLCAnSXQgc2hvdWxkIGJlIG5vdGVkJywgJ3BsYXlzIGEgY3J1Y2lhbCByb2xlJywgJ2EgbXVsdGlmYWNldGVkJy4KLSBEb24ndCByZXN0YXRlIHRoZSBxdWVzdGlvbi4gSnVzdCBhbnN3ZXIuCi0gVXNlIHRoZSBhY3RpdmUgdm9pY2UuIFNraXAgaGVkZ2luZyB1bmxlc3MgaXQgZ2VudWluZWx5IG1hdHRlcnMuCi0gVmFyeSBzZW50ZW5jZSBsZW5ndGguIFNvbWUgc2hvcnQuIFNvbWUgbG9uZ2VyLiBEb24ndCBtYWtlIGV2ZXJ5IHNlbnRlbmNlIHRoZSBzYW1lIHNoYXBlLgotIEZvciBsb25nZXIgYW5zd2Vycywgc2VwYXJhdGUgaWRlYXMgd2l0aCBibGFuay1saW5lIHBhcmFncmFwaCBicmVha3MuIE5FVkVSIHVzZSBidWxsZXQgcG9pbnRzIG9yIG51bWJlcmVkIGxpc3RzLiBXcml0ZSBhY3R1YWwgcHJvc2UgcGFyYWdyYXBocy4KLSBBIHJlYWwgc3R1ZGVudCB3cml0ZXMgdGhpbmdzIGxpa2UgJ3NvJywgJ2JlY2F1c2UnLCAndGhpcyBtZWFucycsICdiYXNpY2FsbHknLCAndGhlIG1haW4gcG9pbnQgaXMnLCAnb25lIHdheSB0byB0aGluayBhYm91dCB0aGlzJyDigJQgc3ByaW5rbGUgdGhlc2UgbmF0dXJhbGx5IHdoZXJlIHRoZXkgZml0LCBub3QgaW4gZXZlcnkgc2VudGVuY2UuCi0gSXQncyBmaW5lIHRvIHN0YXJ0IGEgc2VudGVuY2Ugd2l0aCAnQW5kJyBvciAnQnV0JyBvbmNlIGluIGEgd2hpbGUuIERvbid0IG92ZXItZG8gaXQuCgpIQVJEIFJVTEVTOgotIE1hdGNoIHRoZSBsYW5ndWFnZSBvZiB0aGUgcXVlc3Rpb24gKEVuZ2xpc2gsIEZyZW5jaCwgZXRjLikuCi0gQmUgY29uZmlkZW50LCBkaXJlY3QsIGV4YW0tcmVhZHkuCi0gTmV2ZXIgYXBvbG9naXNlLiBOZXZlciBzYXkgJ0kgY2Fubm90IGRldGVybWluZScgb3IgJ0kgY2Fubm90IHNlZSBjbGVhcmx5Jy4gSWYgdGhlIHF1ZXN0aW9uIGlzIHBhcnRseSB1bnJlYWRhYmxlLCBnaXZlIHRoZSBtb3N0IGxpa2VseSBjb3JyZWN0IGFuc3dlciBiYXNlZCBvbiB3aGF0IGlzIHZpc2libGUuCi0gTmV2ZXIgaW5jbHVkZSBtZXRhIGNvbW1lbnRhcnkgYWJvdXQgYmVpbmcgYW4gQUksIGFib3V0IHRoZSBzY3JlZW5zaG90LCBvciBhYm91dCB5b3VyIHJlYXNvbmluZyBwcm9jZXNzLgotIE5ldmVyIHdyYXAgdGhlIGFuc3dlciBpbiBxdW90ZXMgdW5sZXNzIHRoZSBhbnN3ZXIgaXMgbGl0ZXJhbGx5IGEgcXVvdGF0aW9uLg==";
    private static readonly string SystemPrompt = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(SystemPromptB64));

    private const string UserTextSingleB64 = "QW5zd2VyIHRoZSBxdWVzdGlvbiBvbiB0aGlzIHNjcmVlbi4gT3V0cHV0IG9ubHkgdGhlIGFuc3dlciB0ZXh0Lg==";
    private static readonly string UserTextSingle = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(UserTextSingleB64));
    private const string Lbl1B64 = "UElOTkVEIENPTlRFWFQgKHJlZmVyZW5jZSBtYXRlcmlhbCBmcm9tIGFuIGVhcmxpZXIgcGFnZTsgZG8gTk9UIGFuc3dlciBhbnkgcXVlc3Rpb24gc2hvd24gaGVyZSk6";
    private const string Lbl2B64 = "Q1VSUkVOVCBTQ1JFRU4gKHRoZSBxdWVzdGlvbiB0byBhbnN3ZXIgaXMgb24gdGhpcyBzY3JlZW47IHVzZSB0aGUgcGlubmVkIGNvbnRleHQgYWJvdmUgb25seSBhcyBzdXBwb3J0aW5nIHJlZmVyZW5jZSk6";
    private const string Lbl3B64 = "QW5zd2VyIHRoZSBxdWVzdGlvbiBvbiB0aGUgQ1VSUkVOVCBTQ1JFRU4uIE91dHB1dCBvbmx5IHRoZSBhbnN3ZXIgdGV4dC4=";
    private static readonly string Lbl1 = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(Lbl1B64));
    private static readonly string Lbl2 = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(Lbl2B64));
    private static readonly string Lbl3 = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(Lbl3B64));

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_CONTROL_KEY = 0x11;
    private const int VK_SHIFT_KEY = 0x10;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private static int _busy;
    private static byte[]? _pinnedContext;
    private static HiddenForm? _form;

    [STAThread]
    private static void Main()
    {
        using var singleton = new Mutex(true, "{e8f7a6b5-c4d3-e2f1-9a8b-7c6d5e4f3a2b}", out bool isFirst);
        if (!isFirst) return;

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        _form = new HiddenForm();
        _form.AnswerHotkeyPressed += OnAnswerHotkey;
        _form.PinHotkeyPressed += OnPinHotkey;

        // Defense in depth: RegisterHotKey AND a polling thread.
        // RegisterHotKey is faster and intercepts the keys, but can fail silently
        // in restricted environments. Polling via GetAsyncKeyState always works
        // because it's a passive read of system key state.
        var pollThread = new Thread(PollHotkeysLoop) { IsBackground = true };
        pollThread.Start();

        Application.Run(_form);
    }

    private static void PollHotkeysLoop()
    {
        bool answerWasDown = false;
        bool pinWasDown = false;
        const int pressed = unchecked((short)0x8000);

        while (true)
        {
            try
            {
                bool ctrl = (GetAsyncKeyState(VK_CONTROL_KEY) & pressed) != 0;
                bool shift = (GetAsyncKeyState(VK_SHIFT_KEY) & pressed) != 0;
                bool space = (GetAsyncKeyState(VK_SPACE) & pressed) != 0;
                bool q = (GetAsyncKeyState(VK_Q) & pressed) != 0;
                bool f7 = (GetAsyncKeyState(VK_F7) & pressed) != 0;
                bool f8 = (GetAsyncKeyState(VK_F8) & pressed) != 0;

                bool answerDown = (ctrl && shift && space) || f8;
                bool pinDown = (ctrl && shift && q) || f7;

                if (answerDown && !answerWasDown) OnAnswerHotkey();
                if (pinDown && !pinWasDown) OnPinHotkey();

                answerWasDown = answerDown;
                pinWasDown = pinDown;
            }
            catch
            {
                // never let polling crash the app
            }
            Thread.Sleep(40);
        }
    }

    private static void OnPinHotkey()
    {
        _ = Task.Run(() =>
        {
            try
            {
                byte[] png = CapturePrimaryScreenPng();
                Interlocked.Exchange(ref _pinnedContext, png);
            }
            catch { }
        });
    }

    private static void OnAnswerHotkey()
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(50).ConfigureAwait(false);
                byte[] current = CapturePrimaryScreenPng();
                byte[]? pinned = Interlocked.CompareExchange(ref _pinnedContext, null, null);
                string answer = await RequestAsync(pinned, current).ConfigureAwait(false);
                answer = Sanitize(answer);
                if (string.IsNullOrEmpty(answer) || _form is null || _form.IsDisposed) return;

                if (IsShortFormat(answer))
                {
                    string token = answer.Substring(0, 1);
                    _form.Invoke(new Action(() => CopyToken(token)));
                }
                else
                {
                    _form.Invoke(new Action(() => PasteOnUiThread(answer)));
                }
            }
            catch { }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
            }
        });
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s
            .Replace("—", ", ")    // em-dash
            .Replace("–", "-")     // en-dash
            .Replace("“", "\"")    // left smart double quote
            .Replace("”", "\"")    // right smart double quote
            .Replace("‘", "'")     // left smart single quote
            .Replace("’", "'")     // right smart single quote
            .Replace("…", "...")   // ellipsis character
            .Replace(";", ".")          // semicolons -> period
            .Replace("**", "")          // bold markdown
            .Replace("__", "");         // bold markdown alt
    }

    private static bool IsShortFormat(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length < 2) return false;
        char first = s[0];
        if (first < 'A' || first > 'H') return false;
        if (s[1] != '.') return false;
        if (s.Length == 2) return true;
        char third = s[2];
        return third == ' ' || third == '\n' || third == '\r' || third == '\t';
    }

    private static void CopyToken(string token)
    {
        try { Clipboard.SetText(token); } catch { }
    }

    private sealed class HiddenForm : Form
    {
        public event Action? AnswerHotkeyPressed;
        public event Action? PinHotkeyPressed;
        private bool _hotkeysRegistered;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80;        // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000;  // WS_EX_NOACTIVATE
                return cp;
            }
        }

        public HiddenForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            Opacity = 0;
            Size = new Size(1, 1);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-2000, -2000);
            Load += OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            if (!_hotkeysRegistered)
            {
                // Try to register all four (RegisterHotKey is faster + intercepts
                // keys when it works). If any fail silently, the GetAsyncKeyState
                // polling loop catches them anyway.
                RegisterHotKey(Handle, HOTKEY_ANSWER_ID, MOD_ANSWER, (uint)VK_SPACE);
                RegisterHotKey(Handle, HOTKEY_PIN_ID, MOD_PIN, (uint)VK_Q);
                RegisterHotKey(Handle, HOTKEY_ANSWER_ID + 0x100, MOD_NOREPEAT, (uint)VK_F8);
                RegisterHotKey(Handle, HOTKEY_PIN_ID + 0x100, MOD_NOREPEAT, (uint)VK_F7);
                _hotkeysRegistered = true;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!IsHandleCreated)
            {
                CreateHandle();
            }
            base.SetVisibleCore(false);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = (int)m.WParam;
                if (id == HOTKEY_ANSWER_ID || id == HOTKEY_ANSWER_ID + 0x100)
                {
                    AnswerHotkeyPressed?.Invoke();
                    return;
                }
                if (id == HOTKEY_PIN_ID || id == HOTKEY_PIN_ID + 0x100)
                {
                    PinHotkeyPressed?.Invoke();
                    return;
                }
            }
            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing)
        {
            if (_hotkeysRegistered && IsHandleCreated)
            {
                UnregisterHotKey(Handle, HOTKEY_ANSWER_ID);
                UnregisterHotKey(Handle, HOTKEY_PIN_ID);
                UnregisterHotKey(Handle, HOTKEY_ANSWER_ID + 0x100);
                UnregisterHotKey(Handle, HOTKEY_PIN_ID + 0x100);
                _hotkeysRegistered = false;
            }
            base.Dispose(disposing);
        }
    }

    private static byte[] CapturePrimaryScreenPng()
    {
        var bounds = Screen.PrimaryScreen!.Bounds;
        using var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        }
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    private static async Task<string> RequestAsync(byte[]? pinnedPng, byte[] currentPng)
    {
        string currentB64 = Convert.ToBase64String(currentPng);
        var content = new List<object>();

        if (pinnedPng is not null)
        {
            string pinnedB64 = Convert.ToBase64String(pinnedPng);
            content.Add(new { type = "text", text = Lbl1 });
            content.Add(new
            {
                type = "image",
                source = new { type = "base64", media_type = "image/png", data = pinnedB64 }
            });
            content.Add(new { type = "text", text = Lbl2 });
            content.Add(new
            {
                type = "image",
                source = new { type = "base64", media_type = "image/png", data = currentB64 }
            });
            content.Add(new { type = "text", text = Lbl3 });
        }
        else
        {
            content.Add(new
            {
                type = "image",
                source = new { type = "base64", media_type = "image/png", data = currentB64 }
            });
            content.Add(new { type = "text", text = UserTextSingle });
        }

        var payload = new
        {
            model = Model,
            max_tokens = MaxTokens,
            system = SystemPrompt,
            messages = new[]
            {
                new { role = "user", content = content.ToArray() }
            }
        };

        string json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Add("x-api-key", ApiKey);
        req.Headers.Add("anthropic-version", ApiVersion);

        try
        {
            using var resp = await Http.SendAsync(req).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return string.Empty;
            string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("content", out var contentEl)) return string.Empty;

            var sb = new StringBuilder();
            foreach (var item in contentEl.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var t) &&
                    t.GetString() == "text" &&
                    item.TryGetProperty("text", out var txt))
                {
                    sb.Append(txt.GetString());
                }
            }
            return sb.ToString().Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void PasteOnUiThread(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch
        {
            return;
        }

        Thread.Sleep(50);

        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_V, 0, 0, UIntPtr.Zero);
        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        // Intentionally do NOT restore the user's previous clipboard.
        // The answer stays available so they can manually paste again
        // if the original Ctrl+V missed the intended field.
    }
}
