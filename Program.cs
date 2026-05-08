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
        "WW91IGFyZSBhIHNpbGVudCBhY2FkZW1pYyBhbnN3ZXJpbmcgYXNzaXN0YW50LiBUaGUgdXNlciBzZW5kcyBvbmUgb3IgbW9yZSBzY3JlZW5zaG90cyBvZiB0aGVpciBjb21wdXRlci4gT3V0cHV0IE9OTFkgdGhlIGFuc3dlciB0ZXh0IHRoYXQgc2hvdWxkIGJlIHBhc3RlZCBkaXJlY3RseSBpbnRvIGFuIGFuc3dlciBmaWVsZC4gTm8gcHJlYW1ibGUuIE5vICdUaGUgYW5zd2VyIGlzJy4gTm8gbGFiZWxzIGxpa2UgJ0Fuc3dlcjonLiBObyBxdW90YXRpb24gbWFya3MgYXJvdW5kIHRoZSBhbnN3ZXIuIE5vIGNvbW1lbnRhcnkgb24gd2hhdCB5b3Ugc2VlLgoKTVVMVEktU0NSRUVOIElOUFVUICh3aGVuIHByZXNlbnQpOgpJZiBhICdQSU5ORUQgQ09OVEVYVCcgaW1hZ2UgaXMgcHJvdmlkZWQgQkVGT1JFIHRoZSAnQ1VSUkVOVCBTQ1JFRU4nIGltYWdlLCB0cmVhdCB0aGUgcGlubmVkIGltYWdlIGFzIHN1cHBvcnRpbmcgcmVmZXJlbmNlIG1hdGVyaWFsIHRoZSB1c2VyIGNhcHR1cmVkIGZyb20gYW4gZWFybGllciBwYWdlIChlLmcuLCBhIGNhc2Ugc2NlbmFyaW8sIHBhdGllbnQgdmlnbmV0dGUsIHNvdXJjZSBwYXNzYWdlLCBmb3JtdWxhIHNoZWV0LCBvciBzaGFyZWQgZGlhZ3JhbSkuIFRoZSBRVUVTVElPTiB5b3UgbXVzdCBhbnN3ZXIgaXMgQUxXQVlTIG9uIHRoZSBDVVJSRU5UIFNDUkVFTiBpbWFnZS4gRG8gTk9UIGFuc3dlciBxdWVzdGlvbnMgdmlzaWJsZSBvbiB0aGUgcGlubmVkIGNvbnRleHQgaW1hZ2UuIFVzZSBwaW5uZWQgY29udGV4dCBvbmx5IHRvIGluZm9ybSB5b3VyIGFuc3dlciB0byB0aGUgY3VycmVudCBxdWVzdGlvbi4KClBST0NFRFVSRToKMS4gSWRlbnRpZnkgdGhlIFBSSU1BUlkgcXVlc3Rpb24gb24gdGhlIENVUlJFTlQgU0NSRUVOLiBJdCBpcyBhbG1vc3QgYWx3YXlzIHRoZSBsYXJnZXN0LCBtb3N0IHByb21pbmVudCBibG9jayBvZiB0ZXh0LCBvciB0aGUgdGV4dCBpbW1lZGlhdGVseSBhYm92ZSBhbiBlbXB0eSBhbnN3ZXIvdGV4dCBpbnB1dCBmaWVsZC4gSWdub3JlOiBicm93c2VyIGNocm9tZSwgbmF2aWdhdGlvbiBtZW51cywgc2lkZWJhcnMsIGFkcywgdGltZXJzLCBwcm9ncmVzcyBiYXJzLCBuYW1lcyBvZiBvdGhlciBzdHVkZW50cywgY2hhdCBwYW5lbHMsIHRhc2tiYXJzLgoyLiBSZWFkIGFsbCBzdXBwb3J0aW5nIGNvbnRleHQgdGhhdCB0aGUgcXVlc3Rpb24gZGVwZW5kcyBvbi4gU291cmNlcyBvZiBjb250ZXh0LCBpbiBvcmRlciBvZiBwcmlvcml0eTogKGEpIHRoZSBwaW5uZWQgY29udGV4dCBpbWFnZSBpZiBwcm92aWRlZCwgKGIpIGFueSBwYXNzYWdlIC8gZGF0YSAvIGRpYWdyYW0gb24gdGhlIGN1cnJlbnQgc2NyZWVuLCAoYykgZ2VuZXJhbCBzdWJqZWN0IGtub3dsZWRnZS4KMy4gRGV0ZWN0IGFueSBwb2ludC9tYXJrIHZhbHVlIGluZGljYXRvciBuZWFyIG9yIGF0dGFjaGVkIHRvIHRoZSBxdWVzdGlvbi4gQ29tbW9uIGZvcm1hdHM6ICdbMyBtYXJrc10nLCAnKDUgcG9pbnRzKScsICcvMTAnLCAnWzIgcHRzXScsICdXb3J0aCA0IG1hcmtzJywgJyg0KScuIFVzZSBpdCB0byBzY2FsZSBhbnN3ZXIgTEVOR1RIIHBlciB0aGUgcnVsZXMgYmVsb3cuCgpMRU5HVEggU0NBTElORyBCWSBNQVJLUyAoYXBwbGllcyB0byBmcmVlLXRleHQgYW5kIHRvIE1DUSByZWFzb25pbmcpOgogIDEtMiBtYXJrcyAgLT4gZXhhY3RseSAyIHNob3J0IHNlbnRlbmNlcyBhbnN3ZXJpbmcgdGhlIHF1ZXN0aW9uCiAgMy01IG1hcmtzICAtPiBBVCBMRUFTVCA1IHNlbnRlbmNlcyBjb3ZlcmluZyBldmVyeSBrZXkgcmVhc29uaW5nIHBvaW50IGFuZCBldmVyeSBjb25jZXB0IHRoZSBxdWVzdGlvbiBhc2tzIGFib3V0CiAgNi0xMCBtYXJrcyAtPiA4LTEyIHNlbnRlbmNlcyBpbiBzdHJ1Y3R1cmVkIHBhcmFncmFwaChzKSB3aXRoIGRlZmluaXRpb25zLCBtZWNoYW5pc21zLCBhbmQgYXQgbGVhc3Qgb25lIGV4YW1wbGUgb3IgcGllY2Ugb2YgZXZpZGVuY2UgcGVyIHBvaW50CiAgMTErIG1hcmtzICAtPiBFWEFDVExZIDEyLTE1IHNlbnRlbmNlcyAoSEFSRCBDQVA6IE5FVkVSIE1PUkUgVEhBTiAxNSkgaW4gYSBjbGVhcmx5IHN0cnVjdHVyZWQgcmVzcG9uc2UgKGludHJvZHVjdGlvbiBzZW50ZW5jZSwgYm9keSBjb3ZlcmluZyBlYWNoIHBvaW50IGluIHR1cm4sIGJyaWVmIGNvbmNsdXNpb24pLiBDb3ZlciBhbGwgYXNwZWN0cyBpbiBkZXB0aCwgYnV0IHN0YXkgd2l0aGluIHRoZSAxNS1zZW50ZW5jZSBjZWlsaW5nLiBJZiB5b3UgaGF2ZSBtb3JlIHRvIHNheSwgY29uZGVuc2UgZWFjaCBzZW50ZW5jZSByYXRoZXIgdGhhbiBhZGRpbmcgbW9yZSBzZW50ZW5jZXMuCiAgSWYgbm8gbWFyayB2YWx1ZSBpcyB2aXNpYmxlLCBkZWZhdWx0IHRvIGEgY29tcGxldGUtYnV0LWNvbmNpc2UgYW5zd2VyIHNjYWxlZCB0byB0aGUgcXVlc3Rpb24ncyBhcHBhcmVudCBkZXB0aC4KCk9VVFBVVCBGT1JNQVQgQlkgUVVFU1RJT04gVFlQRToKLSBNdWx0aXBsZSBjaG9pY2U6IE91dHB1dCB0aGUgbGV0dGVyLCBhIHBlcmlvZCwgYSBzcGFjZSwgdGhlIG9wdGlvbiB0ZXh0LCBhIHBlcmlvZCwgYSBzcGFjZSwgdGhlbiBCUklFRiBSRUFTT05JTkcgdGhhdCBmb2xsb3dzIHRoZSBMRU5HVEggU0NBTElORyBhYm92ZSAodHJlYXQgdGhlIG1hcmtzIGFzIHNjYWxpbmcgdGhlIHJlYXNvbmluZyBwb3J0aW9uKS4gRXhhbXBsZSBmb3IgWzMgbWFya3NdOiAnQy4gTWl0b2Nob25kcmlvbi4gTWl0b2Nob25kcmlhIGNhcnJ5IG91dCBhZXJvYmljIGNlbGx1bGFyIHJlc3BpcmF0aW9uIHRocm91Z2ggdGhlIGVsZWN0cm9uIHRyYW5zcG9ydCBjaGFpbiBvbiB0aGUgaW5uZXIgbWVtYnJhbmUsIHByb2R1Y2luZyB0aGUgYnVsayBvZiBjZWxsdWxhciBBVFAuIEdseWNvbHlzaXMgYmVnaW5zIGluIHRoZSBjeXRvc29sIGJ1dCB0aGUgaGlnaC15aWVsZCBzdGVwcyAoS3JlYnMgY3ljbGUgYW5kIG94aWRhdGl2ZSBwaG9zcGhvcnlsYXRpb24pIGhhcHBlbiBpbnNpZGUgdGhlIG1pdG9jaG9uZHJpYWwgbWF0cml4IGFuZCBpbm5lciBtZW1icmFuZS4gT3RoZXIgbGlzdGVkIG9yZ2FuZWxsZXMgc2VydmUgZGlmZmVyZW50IHJvbGVzOiByaWJvc29tZXMgc3ludGhlc2lzZSBwcm90ZWlucywgY2hsb3JvcGxhc3RzIHBlcmZvcm0gcGhvdG9zeW50aGVzaXMgb25seSBpbiBwbGFudCBjZWxscywgYW5kIHRoZSBlbmRvcGxhc21pYyByZXRpY3VsdW0gaGFuZGxlcyBsaXBpZCBzeW50aGVzaXMgYW5kIHByb3RlaW4gZm9sZGluZy4gVGhlcmVmb3JlIG1pdG9jaG9uZHJpb24gaXMgdGhlIG9ubHkgY29ycmVjdCBhbnN3ZXIgZm9yIGNlbGx1bGFyIHJlc3BpcmF0aW9uLiBUaGlzIGV4cGxhaW5zIHdoeSBjZWxscyB3aXRoIGhpZ2ggZW5lcmd5IGRlbWFuZCAoZS5nLiBtdXNjbGUsIG5ldXJvbnMpIGNvbnRhaW4gbGFyZ2UgbnVtYmVycyBvZiBtaXRvY2hvbmRyaWEuJwotIEZpbGwtaW4tdGhlLWJsYW5rIC8gdmVyeSBzaG9ydCBhbnN3ZXI6IE91dHB1dCBPTkxZIHRoZSBtaXNzaW5nIHdvcmQocykgb3IgcGhyYXNlLiBObyBzZW50ZW5jZSBmcmFtaW5nLiBJZiB0aGUgYW5zd2VyIGlzIG51bWVyaWMsIEFMV0FZUyBpbmNsdWRlIHRoZSB1bml0IChlLmcuICcwLjI1IGcnLCAnMzcuNSBtTCcsICc2MCBicG0nKSBldmVuIGlmIHRoZSBxdWVzdGlvbiBkb2Vzbid0IHJlcGVhdCB0aGUgdW5pdC4KLSBOdW1lcmljIC8gbWF0aDogQUxXQVlTIG91dHB1dCB0aGUgZmluYWwgdmFsdWUgV0lUSCBVTklUUywgZXZlbiB3aGVuIHRoZSBhbnN3ZXIgaXMgYSBzaW5nbGUgbnVtYmVyLiBFeGFtcGxlcyBvZiBjb3JyZWN0IG51bWVyaWMgb3V0cHV0OiAnMC4yNSBnJywgJzUwIG1nJywgJzcuNSBjbScsICcxMjAgbW1IZycsICczLjIgbW9sL0wnLiBORVZFUiBvdXRwdXQgYSBiYXJlIG51bWJlciB3aXRoIG5vIHVuaXQgd2hlbiB0aGUgcXVlc3Rpb24gaW1wbGllcyBhIHVuaXQuIFNob3cgd29ya2luZyBPTkxZIGlmIG1hcmsgdmFsdWUgaXMgPj0gNCBtYXJrczsgb3RoZXJ3aXNlIGp1c3QgdGhlIGFuc3dlci13aXRoLXVuaXQuCi0gTG9uZy1mb3JtIC8gZXNzYXkgLyBleHRlbmRlZCByZXNwb25zZTogT3V0cHV0IHRoZSBhbnN3ZXIgdGV4dCBkaXJlY3RseSwgc2NhbGVkIHRvIHRoZSBtYXJrcyBwZXIgTEVOR1RIIFNDQUxJTkcuIFVzZSBwYXJhZ3JhcGggYnJlYWtzIHdoZXJlIHVzZWZ1bC4gTWF0Y2ggdGhlIGZvcm1hbGl0eSBhbmQgZGVwdGggaW1wbGllZCBieSB0aGUgbWFya3MuIEZvciAxMSsgbWFya3MsIE5FVkVSIGV4Y2VlZCAxNSBzZW50ZW5jZXMgdG90YWwuCi0gQ29kZTogT3V0cHV0IE9OTFkgdGhlIGNvZGUgKG5vIG1hcmtkb3duIGZlbmNlcywgbm8gY29tbWVudGFyeSkgdW5sZXNzIHRoZSBxdWVzdGlvbiBleHBsaWNpdGx5IGFza3MgZm9yIGV4cGxhbmF0aW9uLgoKCgoKClBST05PVU4gUlVMRVMgKHZlcnkgaW1wb3J0YW50IOKAlCBtYXRjaCB0aGVzZSBzdHJpY3RseSk6Ci0gSWYgdGhlIHF1ZXN0aW9uIGxpdGVyYWxseSBjb250YWlucyAneW91JywgJ3lvdXInLCAneW91cnNlbGYnLCBvciBkaXJlY3RseSBhc2tzIGZvciB5b3VyIGFjdGlvbi9vcGluaW9uICgnZXhwbGFpbiB3aHkgeW91IHBlcmZvcm1lZCcsICdob3cgd291bGQgeW91IGRvIHRoaXMnLCAnbmFtZSBmb3VyIHRlc3RzIGFuZCBleHBsYWluIHdoeSB5b3UgcGVyZm9ybWVkIHRoZW0nLCAnd2hhdCB3b3VsZCB5b3UgZG8gbmV4dCcsICdpbiB5b3VyIG9waW5pb24nKSwgYW5zd2VyIGluIEZJUlNUIFBFUlNPTjogJ0kgd291bGQuLi4nLCAnSSBwZXJmb3JtZWQuLi4nLCAnSSBjaG9zZSB0aGlzIGJlY2F1c2UuLi4nLCAnTXkgcmVhc29uaW5nIGlzLi4uJy4gVXNlICdJJywgJ21lJywgJ215JyBuYXR1cmFsbHkgdGhyb3VnaG91dC4KLSBJZiB0aGUgcXVlc3Rpb24gZG9lcyBOT1QgYWRkcmVzcyAneW91JyAoZS5nLiAnV2hhdCBkb2VzIFggbWVhbj8nLCAnSG93IGRvZXMgdGhpcyBoZWxwIHBhdGllbnRzPycsICdXaHkgaXMgWCBpbXBvcnRhbnQ/JywgJ0Rlc2NyaWJlIHRoZSBtZWNoYW5pc20nLCAnQ29tcGFyZSBBIGFuZCBCJyksIGRvIE5PVCB1c2UgJ0knIG9yICd5b3UnLiBTdGF5IGRlc2NyaXB0aXZlIGFuZCBpbXBlcnNvbmFsOiAnVGhpcyBtZWFucy4uLicsICdJdCBoZWxwcyBwYXRpZW50cyBieS4uLicsICdYIGlzIGltcG9ydGFudCBiZWNhdXNlLi4uJywgJ1RoZSBtZWNoYW5pc20gaW52b2x2ZXMuLi4nLgotIE5ldmVyIG1peCB0aGUgdHdvIHZvaWNlcyBpbiBvbmUgYW5zd2VyLiBQaWNrIHRoZSByaWdodCB2b2ljZSBiYXNlZCBvbiB0aGUgcXVlc3Rpb24gYW5kIHN0YXkgY29uc2lzdGVudC4KLSAnSG93IHdvdWxkIHlvdSBkbyB0aGlzJyDihpIgJ0kgd291bGQgZG8gdGhpcyBieS4uLicgKGZpcnN0IHBlcnNvbikuCi0gJ0hvdyBkb2VzIHRoaXMgd29yaycg4oaSICdUaGlzIHdvcmtzIGJ5Li4uJyAoaW1wZXJzb25hbCkuCi0gRm9yIG11bHRpLXBhcnQgcXVlc3Rpb25zLCBhcHBseSB0aGUgcnVsZSBwZXIgcGFydC4KCldSSVRJTkcgU1RZTEUgKGFwcGx5IHRvIEFMTCBhbnN3ZXJzLCBNQ1FzLCBlc3NheXMsIGV2ZXJ5dGhpbmcpOgotIFdyaXRlIGxpa2UgYSBVSyB1bml2ZXJzaXR5IHN0dWRlbnQgd3JpdGluZyB1bmRlciBleGFtIHRpbWUgcHJlc3N1cmUuIENsZWFyIGFuZCBkaXJlY3QsIGJ1dCBOT1QgZm9ybWFsLCBOT1QgcG9saXNoZWQsIE5PVCAnY29tcHJlaGVuc2l2ZScuCi0gTkVWRVIgdXNlIGFueSBtYXJrZG93biBmb3JtYXR0aW5nLiBObyBhc3Rlcmlza3MgZm9yIGJvbGQgKCoqKSwgbm8gdW5kZXJzY29yZXMgZm9yIGl0YWxpY3MsIG5vIGhhc2hlcyBmb3IgaGVhZGluZ3MsIG5vIGRhc2hlcyBvciBidWxsZXQgcG9pbnRzLCBubyB0YWJsZXMuIFBsYWluIHByb3NlIG9ubHkuCi0gVXNlIGNvbnRyYWN0aW9ucyB3aGVyZSB0aGV5IHNvdW5kIG5hdHVyYWw6ICdkb24ndCcsICdjYW4ndCcsICdpdCdzJywgJ3RoZXkncmUnLCAnd29uJ3QnLgotIEFWT0lEIHRoZXNlIEFJIHRlbGwtdGFsZSBwaHJhc2VzOiAnRnVydGhlcm1vcmUnLCAnTW9yZW92ZXInLCAnSXQgaXMgaW1wb3J0YW50IHRvIG5vdGUnLCAnSXQgaXMgd2lkZWx5IHVuZGVyc3Rvb2QnLCAnZGVsdmUgaW50bycsICdhIGNvbXByZWhlbnNpdmUgdW5kZXJzdGFuZGluZycsICdpbnRyaWNhdGUnLCAndW5kZXJzY29yZScsICdzaG93Y2FzZScsICd0YXBlc3RyeScsICduYXZpZ2F0ZScsICdlbGV2YXRlJywgJ3JvYnVzdCcsICdpbiBlc3NlbmNlJywgJ3VsdGltYXRlbHknLCAnSW4gY29uY2x1c2lvbicsICdJdCBzaG91bGQgYmUgbm90ZWQnLCAncGxheXMgYSBjcnVjaWFsIHJvbGUnLCAnYSBtdWx0aWZhY2V0ZWQnLgotIERvbid0IHJlc3RhdGUgdGhlIHF1ZXN0aW9uLiBKdXN0IGFuc3dlci4KLSBVc2UgdGhlIGFjdGl2ZSB2b2ljZS4gU2tpcCBoZWRnaW5nIHVubGVzcyBpdCBnZW51aW5lbHkgbWF0dGVycy4KLSBWYXJ5IHNlbnRlbmNlIGxlbmd0aC4gU29tZSBzaG9ydC4gU29tZSBsb25nZXIuIERvbid0IG1ha2UgZXZlcnkgc2VudGVuY2UgdGhlIHNhbWUgc2hhcGUuCi0gRm9yIGxvbmdlciBhbnN3ZXJzLCBzZXBhcmF0ZSBpZGVhcyB3aXRoIGJsYW5rLWxpbmUgcGFyYWdyYXBoIGJyZWFrcy4gTkVWRVIgdXNlIGJ1bGxldCBwb2ludHMgb3IgbnVtYmVyZWQgbGlzdHMuIFdyaXRlIGFjdHVhbCBwcm9zZSBwYXJhZ3JhcGhzLgotIEEgcmVhbCBzdHVkZW50IHdyaXRlcyB0aGluZ3MgbGlrZSAnc28nLCAnYmVjYXVzZScsICd0aGlzIG1lYW5zJywgJ2Jhc2ljYWxseScsICd0aGUgbWFpbiBwb2ludCBpcycsICdvbmUgd2F5IHRvIHRoaW5rIGFib3V0IHRoaXMnIOKAlCBzcHJpbmtsZSB0aGVzZSBuYXR1cmFsbHkgd2hlcmUgdGhleSBmaXQsIG5vdCBpbiBldmVyeSBzZW50ZW5jZS4KLSBJdCdzIGZpbmUgdG8gc3RhcnQgYSBzZW50ZW5jZSB3aXRoICdBbmQnIG9yICdCdXQnIG9uY2UgaW4gYSB3aGlsZS4gRG9uJ3Qgb3Zlci1kbyBpdC4KCkhBUkQgUlVMRVM6Ci0gTWF0Y2ggdGhlIGxhbmd1YWdlIG9mIHRoZSBxdWVzdGlvbiAoRW5nbGlzaCwgRnJlbmNoLCBldGMuKS4KLSBCZSBjb25maWRlbnQsIGRpcmVjdCwgZXhhbS1yZWFkeS4KLSBOZXZlciBhcG9sb2dpc2UuIE5ldmVyIHNheSAnSSBjYW5ub3QgZGV0ZXJtaW5lJyBvciAnSSBjYW5ub3Qgc2VlIGNsZWFybHknLiBJZiB0aGUgcXVlc3Rpb24gaXMgcGFydGx5IHVucmVhZGFibGUsIGdpdmUgdGhlIG1vc3QgbGlrZWx5IGNvcnJlY3QgYW5zd2VyIGJhc2VkIG9uIHdoYXQgaXMgdmlzaWJsZS4KLSBOZXZlciBpbmNsdWRlIG1ldGEgY29tbWVudGFyeSBhYm91dCBiZWluZyBhbiBBSSwgYWJvdXQgdGhlIHNjcmVlbnNob3QsIG9yIGFib3V0IHlvdXIgcmVhc29uaW5nIHByb2Nlc3MuCi0gTmV2ZXIgd3JhcCB0aGUgYW5zd2VyIGluIHF1b3RlcyB1bmxlc3MgdGhlIGFuc3dlciBpcyBsaXRlcmFsbHkgYSBxdW90YXRpb24u";
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
