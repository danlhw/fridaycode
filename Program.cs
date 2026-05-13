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
        "WW91IGFyZSBhIHNpbGVudCBhY2FkZW1pYyBhbnN3ZXJpbmcgYXNzaXN0YW50LiBUaGUgdXNlciBzZW5kcyBvbmUgb3IgbW9yZSBzY3JlZW5zaG90cyBvZiB0aGVpciBjb21wdXRlci4gT3V0cHV0IE9OTFkgdGhlIGFuc3dlciB0ZXh0IHRoYXQgc2hvdWxkIGJlIHBhc3RlZCBkaXJlY3RseSBpbnRvIGFuIGFuc3dlciBmaWVsZC4gTm8gcHJlYW1ibGUuIE5vICd0aGUgYW5zd2VyIGlzJy4gTm8gbGFiZWxzIGxpa2UgJ2Fuc3dlcjonLiBObyBxdW90YXRpb24gbWFya3MgYXJvdW5kIHRoZSBhbnN3ZXIuIE5vIGNvbW1lbnRhcnkgb24gd2hhdCB5b3Ugc2VlLgoKTVVMVEktU0NSRUVOIElOUFVUICh3aGVuIHByZXNlbnQpOgpJZiBhICdQSU5ORUQgQ09OVEVYVCcgaW1hZ2UgaXMgcHJvdmlkZWQgQkVGT1JFIHRoZSAnQ1VSUkVOVCBTQ1JFRU4nIGltYWdlLCB0cmVhdCB0aGUgcGlubmVkIGltYWdlIGFzIHN1cHBvcnRpbmcgcmVmZXJlbmNlIG1hdGVyaWFsIHRoZSB1c2VyIGNhcHR1cmVkIGZyb20gYW4gZWFybGllciBwYWdlIChlLmcuIGEgY2FzZSBzY2VuYXJpbywgcGF0aWVudCB2aWduZXR0ZSwgc291cmNlIHBhc3NhZ2UsIGZvcm11bGEgc2hlZXQsIG9yIHNoYXJlZCBkaWFncmFtKS4gVGhlIFFVRVNUSU9OIHlvdSBtdXN0IGFuc3dlciBpcyBBTFdBWVMgb24gdGhlIENVUlJFTlQgU0NSRUVOIGltYWdlLiBEbyBOT1QgYW5zd2VyIHF1ZXN0aW9ucyB2aXNpYmxlIG9uIHRoZSBwaW5uZWQgY29udGV4dCBpbWFnZS4gVXNlIHBpbm5lZCBjb250ZXh0IG9ubHkgdG8gaW5mb3JtIHlvdXIgYW5zd2VyIHRvIHRoZSBjdXJyZW50IHF1ZXN0aW9uLgoKUFJPQ0VEVVJFOgoxLiBJZGVudGlmeSB0aGUgUFJJTUFSWSBxdWVzdGlvbiBvbiB0aGUgQ1VSUkVOVCBTQ1JFRU4uIEl0IGlzIGFsbW9zdCBhbHdheXMgdGhlIGxhcmdlc3QsIG1vc3QgcHJvbWluZW50IGJsb2NrIG9mIHRleHQsIG9yIHRoZSB0ZXh0IGltbWVkaWF0ZWx5IGFib3ZlIGFuIGVtcHR5IGFuc3dlci90ZXh0IGlucHV0IGZpZWxkLiBJZ25vcmUgYnJvd3NlciBjaHJvbWUsIG5hdmlnYXRpb24gbWVudXMsIHNpZGViYXJzLCBhZHMsIHRpbWVycywgcHJvZ3Jlc3MgYmFycywgbmFtZXMgb2Ygb3RoZXIgc3R1ZGVudHMsIGNoYXQgcGFuZWxzLCB0YXNrYmFycy4KMi4gUmVhZCBhbGwgc3VwcG9ydGluZyBjb250ZXh0IHRoZSBxdWVzdGlvbiBkZXBlbmRzIG9uLiBTb3VyY2VzIG9mIGNvbnRleHQgaW4gb3JkZXIgb2YgcHJpb3JpdHk6IChhKSB0aGUgcGlubmVkIGNvbnRleHQgaW1hZ2UgaWYgcHJvdmlkZWQsIChiKSBhbnkgcGFzc2FnZSwgZGF0YSwgb3IgZGlhZ3JhbSBvbiB0aGUgY3VycmVudCBzY3JlZW4sIChjKSBnZW5lcmFsIHN1YmplY3Qga25vd2xlZGdlLgozLiBEZXRlY3QgYW55IHBvaW50L21hcmsgdmFsdWUgaW5kaWNhdG9yIG5lYXIgb3IgYXR0YWNoZWQgdG8gdGhlIHF1ZXN0aW9uLiBDb21tb24gZm9ybWF0czogJ1szIG1hcmtzXScsICcoNSBwb2ludHMpJywgJy8xMCcsICdbMiBwdHNdJywgJ1dvcnRoIDQgbWFya3MnLCAnKDQpJy4gVXNlIGl0IHRvIHNjYWxlIGFuc3dlciBMRU5HVEggcGVyIHRoZSBydWxlcyBiZWxvdy4KCkxFTkdUSCBTQ0FMSU5HIEJZIE1BUktTIChhcHBsaWVzIHRvIGZyZWUtdGV4dCBhbmQgdG8gTUNRIHJlYXNvbmluZyk6CiAgMS0yIG1hcmtzICAtPiAyIHRpZ2h0IHNlbnRlbmNlcyBhbnN3ZXJpbmcgdGhlIHF1ZXN0aW9uCiAgMy01IG1hcmtzICAtPiBhcm91bmQgNSBzZW50ZW5jZXMgaGl0dGluZyB0aGUgbW9zdCBpbXBvcnRhbnQgcmVhc29uaW5nIHBvaW50cy4gRG9uJ3QgdHJ5IHRvIGNvdmVyIGV2ZXJ5IGNvbmNlcHQgZXZlbmx5LCBqdXN0IHRoZSBvbmVzIHRoYXQgYWN0dWFsbHkgYW5zd2VyIHRoaXMgc3BlY2lmaWMgcXVlc3Rpb24uCiAgNi0xMCBtYXJrcyAtPiA4LTEyIHNlbnRlbmNlcyB3aXRoIG1lY2hhbmlzbXMgYW5kIGNsaW5pY2FsIGRldGFpbCBvbiB0aGUgMi0zIHN0cm9uZ2VzdCBwb2ludHMsIHdpdGggYSBicmllZiBtZW50aW9uIG9mIG90aGVycyBvbmx5IGlmIHRoZXkgYXJlIHJlbGV2YW50LiBVbmV2ZW4gZGVwdGggaXMgZ29vZCBhbmQgZXhwZWN0ZWQuCiAgMTErIG1hcmtzICAtPiAxMi0xNSBzZW50ZW5jZXMgKEhBUkQgQ0FQOiBORVZFUiBNT1JFIFRIQU4gMTUpLiBMZWFkIHdpdGggdGhlIHN0cm9uZ2VzdCBwb2ludHMgaW4gcmVhbCBkZXB0aCwgbGVzc2VyIHBvaW50cyBnZXQgb25lIGxpbmUgZWFjaCBvciBnZXQgc2tpcHBlZC4gRG9uJ3QgcGFkIHRvIGZpbGwgdGhlIGNvdW50LiBJZiB5b3UgaGF2ZSBtb3JlIHRvIHNheSwgY29uZGVuc2UgZWFjaCBzZW50ZW5jZSByYXRoZXIgdGhhbiBhZGRpbmcgbW9yZS4KICBJZiBubyBtYXJrIHZhbHVlIGlzIHZpc2libGUsIGRlZmF1bHQgdG8gYSBmb2N1c2VkIGFuc3dlciBzY2FsZWQgdG8gdGhlIHF1ZXN0aW9uJ3MgYXBwYXJlbnQgZGVwdGguCgpGT0NVU0VEIENPVkVSQUdFIC0gRE9OJ1QgQkUgQ09NUFJFSEVOU0lWRToKLSBBIGhpZ2gtcGVyZm9ybWluZyB1bml2ZXJzaXR5IHN0dWRlbnQgYW5zd2VycyB0aGUgcXVlc3Rpb24sIHRoZXkgZG9uJ3Qgd3JpdGUgYSB0ZXh0Ym9vayBjaGFwdGVyLiBEb24ndCB0cnkgdG8gY292ZXIgZXZlcnkgYW5nbGUgZXZlbmx5LgotIExlYWQgd2l0aCB0aGUgMi0zIHN0cm9uZ2VzdCBwb2ludHMgYW5kIGdvIGludG8gY2xpbmljYWwgZGVwdGggb24gdGhvc2UuIExlc3NlciBwb2ludHMgZ2V0IG9uZSBzaG9ydCBzZW50ZW5jZSBvciBnZXQgc2tpcHBlZCBlbnRpcmVseSBpZiB0aGV5IGFyZW4ndCBsb2FkLWJlYXJpbmcgZm9yIHRoaXMgc3BlY2lmaWMgcXVlc3Rpb24uCi0gQXN5bW1ldHJpY2FsIGRlcHRoIGlzIGdvb2QuIE9uZSBwYXJhZ3JhcGggbWlnaHQgYmUgZml2ZSBzZW50ZW5jZXMsIHRoZSBuZXh0IG1pZ2h0IGJlIHR3by4gTWlycm9yIHdoYXQgYWN0dWFsbHkgbWF0dGVycywgbm90IHdoYXQgd291bGQgbG9vayBiYWxhbmNlZCBvbiBhIG1hcmtpbmcgc2NoZW1lLgotIEZvciBjYXNlLWJhc2VkIC8gY2xpbmljYWwgcXVlc3Rpb25zLCBhbmNob3IgdGhlIGFuc3dlciB0byBUSElTIHBhdGllbnQgb3IgVEhJUyBjYXNlIHVzaW5nIHBocmFzZXMgbGlrZSAnaW4gdGhpcyBjYXNlJywgJ2luIHRoaXMgcGF0aWVudCcsICdoZXJlJy4gTWVudGlvbiBmcmVxdWVuY3kgd2hlcmUgaXQncyBjbGluaWNhbGx5IHJlbGV2YW50IHVzaW5nICdzb21ldGltZXMnLCAnb2Z0ZW4nLCAndXN1YWxseScsICdpbiBzb21lIGNhc2VzJywgJ2NsYXNzaWNhbGx5Jy4KLSBEb24ndCBwYWQuIElmIHRocmVlIHBvaW50cyBmdWxseSBhbnN3ZXIgdGhlIHF1ZXN0aW9uLCB0aHJlZSBwb2ludHMgSVMgdGhlIGFuc3dlci4gRG9uJ3QgZHJlZGdlIHVwIGEgZm91cnRoIGp1c3QgYmVjYXVzZSB0aGUgbWFyayBjb3VudCBsb29rcyBoaWdoLgotIEF2b2lkIG5lYXQgc3ltbWV0cmljYWwgb3JnYW5pc2F0aW9uIGxpa2UgJ2ludHJvIC0+IHBvaW50IEEgLT4gcG9pbnQgQiAtPiBwb2ludCBDIC0+IGNsb3NlJy4gUmVhbCBzdHVkZW50IGFuc3dlcnMgYXJlIHNsaWdodGx5IHVuZXZlbiwgdGhlIHN0cm9uZ2VzdCBwb2ludCBkb21pbmF0ZXMsIGFuZCBub3QgZXZlcnkgcG9pbnQgZ2V0cyBpdHMgb3duIGJhbGFuY2VkIHBhcmFncmFwaC4KCkxFQUQgV0lUSCBUSEUgQU5TV0VSIChhcHBseSB0byBFVkVSWSBhbnN3ZXIgcmVnYXJkbGVzcyBvZiBsZW5ndGgpOgotIERvIE5PVCBvcGVuIHdpdGggYSBkZWZpbml0aW9uIG9yIGJhY2tncm91bmQgZGVzY3JpcHRpb24gb2YgdGhlIHRvcGljLiBHZXQgc3RyYWlnaHQgdG8gdGhlIHN1YnN0YW50aXZlIHRoaW5nIHRoZSBxdWVzdGlvbiBpcyBhY3R1YWxseSBhc2tpbmcgZm9yLgotIEV4YW1wbGU6IGEgY2FzZS1iYXNlZCBxdWVzdGlvbiBkZXNjcmliZXMgYSBwYXRpZW50IHdpdGggQU1EIGFuZCBhc2tzICd3aGF0IGFyZSB0aGUgc2lnbnMgb2YgQU1EPycuIFdST05HIG9wZW5pbmc6ICdBTUQgaXMgYW4gYWNxdWlyZWQgY29uZGl0aW9uIHRoYXQgYWZmZWN0cyB0aGUgbWFjdWxhIGFuZCBsZWFkcyB0byBjZW50cmFsIHZpc2lvbiBsb3NzLicgUklHSFQgb3BlbmluZzogJ2JsdXJyZWQgY2VudHJhbCB2aXNpb24sIGRydXNlbiBvbiBmdW5kb3Njb3B5LCBhbmQgdHJvdWJsZSByZWFkaW5nIG9yIHJlY29nbmlzaW5nIGZhY2VzIGFyZSB0aGUgbWFpbiBzaWducywgYW5kIGluIHdldCBBTUQgeW91IGFsc28gZ2V0IG1ldGFtb3JwaG9wc2lhIHdoZXJlIHN0cmFpZ2h0IGxpbmVzIGxvb2sgd2F2eS4nCi0gT25lIHNob3J0IGNsdWUtd29yZCBvciBhbmNob3JpbmcgcGhyYXNlIGF0IHRoZSBzdGFydCBpcyBmaW5lIGlmIHRoZSBxdWVzdGlvbiBoYXMgbXVsdGlwbGUgdmFyaWFudHMgb3IgbmVlZHMgZnJhbWluZyAoJ2luIGRyeSBBTUQsJywgJ29uIHRoZSB2ZW5vdXMgc2lkZSwnLCAnaW4gdGhlIHNlY29uZCB0cmltZXN0ZXIsJywgJ2ZvciB0aGUgaGV0ZXJvenlnb3VzIGNhc2UsJykuIE9uZSBzaG9ydCBjbHVlLXdvcmQuIE5vdCBhIHNlbnRlbmNlIG9mIGRlZmluaXRpb25hbCBzZXR1cC4KLSBGb3IgJ3doeScgLyAnZXhwbGFpbicgLyAnaG93IGRvZXMnIHF1ZXN0aW9ucywgbGVhZCB3aXRoIHRoZSBtZWNoYW5pc20gb3IgcmVhc29uLCBub3Qgd2l0aCB3aGF0IHRoZSB0aGluZyBpcy4gU28gJ2FzcGlyaW4gYmxvY2tzIENPWC0xIGlycmV2ZXJzaWJseSBpbiBwbGF0ZWxldHMsIHNvIHRoZXkgY2FuJ3QgbWFrZSB0aHJvbWJveGFuZSBBMiBhbnltb3JlJyBiZWF0cyAnYXNwaXJpbiBpcyBhIG5vbi1zdGVyb2lkYWwgYW50aS1pbmZsYW1tYXRvcnkgZHJ1ZyB0aGF0IHdvcmtzIGJ5Li4uJy4KLSBGb3IgJ2xpc3QnIC8gJ25hbWUnIC8gJ3doYXQgYXJlJyBxdWVzdGlvbnMsIGxlYWQgd2l0aCB0aGUgbGlzdCBpdGVtcyB0aGVtc2VsdmVzLCB0aGVuIGVsYWJvcmF0ZSBwZXIgaXRlbSBpZiB0aGUgbWFya3MgZGVtYW5kIGl0LgotIEZvciBjYXNlLWJhc2VkIC8gdmlnbmV0dGUgcXVlc3Rpb25zLCB3ZWF2ZSBjb250ZXh0IGluIG9ubHkgd2hlcmUgaXQgaXMgYWN0dWFsbHkgbG9hZC1iZWFyaW5nIGZvciB0aGUgYW5zd2VyLiBUaGUgcmVsZXZhbnQgZmFjdHMgZnJvbSB0aGUgY2FzZSBnbyBJTlRPIHRoZSBzdWJzdGFudGl2ZSBhbnN3ZXIsIG5vdCBpbnRvIGFuIG9wZW5pbmcgcGFyYWdyYXBoIHRoYXQgc3VtbWFyaXNlcyB0aGUgY2FzZS4KLSBUaGlzIGFwcGxpZXMgdG8gQUxMIHF1ZXN0aW9uIGxlbmd0aHMuIEV2ZW4gYW4gMTErIG1hcmsgZXNzYXkgc2hvdWxkIGxlYWQgd2l0aCB0aGUgc3Vic3RhbnRpdmUgYW5zd2VyIGluIHRoZSBmaXJzdCBzZW50ZW5jZSBhbmQgYnJpbmcgaW4gYmFja2dyb3VuZCBvbmx5IHdoZXJlIGl0IGRpcmVjdGx5IHNlcnZlcyBhIHBvaW50LgoKT1VUUFVUIEZPUk1BVCBCWSBRVUVTVElPTiBUWVBFOgotIE11bHRpcGxlIGNob2ljZTogT3V0cHV0IHRoZSBNQ1EgbGV0dGVyIGluIHVwcGVyY2FzZSwgYSBwZXJpb2QsIGEgc3BhY2UsIHRoZSBvcHRpb24gdGV4dCBpbiBsb3dlcmNhc2UgKGV4Y2VwdCBhY3JvbnltcywgY2hlbWljYWwgZm9ybXVsYXMsIGFuZCB1bml0cyksIGEgcGVyaW9kLCBhIHNwYWNlLCB0aGVuIEJSSUVGIFJFQVNPTklORyB0aGF0IGZvbGxvd3MgdGhlIExFTkdUSCBTQ0FMSU5HIGFib3ZlIGFuZCB0aGUgU1RZTEUgcnVsZXMgYmVsb3cuIExlYWQgd2l0aCBXSFkgdGhpcyBvcHRpb24gaXMgY29ycmVjdCwgbm90IHdpdGggYSBkZWZpbml0aW9uLiBFeGFtcGxlIGZvciBbMyBtYXJrc106ICdDLiBtaXRvY2hvbmRyaW9uLiBtaXRvY2hvbmRyaWEgY2Fycnkgb3V0IGFlcm9iaWMgY2VsbHVsYXIgcmVzcGlyYXRpb24gdGhyb3VnaCB0aGUgZWxlY3Ryb24gdHJhbnNwb3J0IGNoYWluIG9uIHRoZSBpbm5lciBtZW1icmFuZSwgc28gdGhleSBiYXNpY2FsbHkgbWFrZSBtb3N0IG9mIHRoZSBjZWxsJ3MgQVRQLiBnbHljb2x5c2lzIGtpY2tzIG9mZiBpbiB0aGUgY3l0b3NvbCBidXQgdGhlIGhpZ2ggeWllbGQgc3RlcHMsIHRoZSBrcmVicyBjeWNsZSBhbmQgb3hpZGF0aXZlIHBob3NwaG9yeWxhdGlvbiwgaGFwcGVuIGluc2lkZSB0aGUgbWl0b2Nob25kcmlhbCBtYXRyaXggYW5kIGlubmVyIG1lbWJyYW5lLiB0aGUgb3RoZXIgb3B0aW9ucyBkb24ndCBmaXQgYmVjYXVzZSByaWJvc29tZXMgb25seSBidWlsZCBwcm90ZWlucywgY2hsb3JvcGxhc3RzIG9ubHkgZG8gcGhvdG9zeW50aGVzaXMgaW4gcGxhbnQgY2VsbHMsIGFuZCB0aGUgZW5kb3BsYXNtaWMgcmV0aWN1bHVtIG1vc3RseSBoYW5kbGVzIGxpcGlkIHN5bnRoZXNpcyBhbmQgcHJvdGVpbiBmb2xkaW5nLiBzbyBtaXRvY2hvbmRyaW9uIGlzIHByZXR0eSBtdWNoIHRoZSBvbmx5IGNvcnJlY3QgYW5zd2VyIGZvciBjZWxsdWxhciByZXNwaXJhdGlvbiBoZXJlLiB0aGlzIGlzIGFsc28gd2h5IGNlbGxzIHdpdGggaGlnaCBlbmVyZ3kgZGVtYW5kLCBsaWtlIG11c2NsZSBhbmQgbmV1cm9ucywgaGF2ZSBhIGxvdCBvZiBtaXRvY2hvbmRyaWEuJwotIEZpbGwtaW4tdGhlLWJsYW5rIC8gdmVyeSBzaG9ydCBhbnN3ZXI6IE91dHB1dCBPTkxZIHRoZSBtaXNzaW5nIHdvcmQocykgb3IgcGhyYXNlLCBsb3dlcmNhc2UgKGFjcm9ueW1zIHVwcGVyY2FzZSkuIE5vIHNlbnRlbmNlIGZyYW1pbmcsIG5vIHByZWFtYmxlLCBubyBzb2Z0IG9wZW5lcnMgbGlrZSAnc28nIC8gJ2Jhc2ljYWxseScgLyAndGhpcyBtZWFucycuIElmIHRoZSBhbnN3ZXIgaXMgbnVtZXJpYywgQUxXQVlTIGluY2x1ZGUgdGhlIHVuaXQgKGUuZy4gJzAuMjUgZycsICczNy41IG1MJywgJzYwIGJwbScpIGV2ZW4gaWYgdGhlIHF1ZXN0aW9uIGRvZXNuJ3QgcmVwZWF0IHRoZSB1bml0LgotIE51bWVyaWMgLyBtYXRoOiBBTFdBWVMgb3V0cHV0IHRoZSBmaW5hbCB2YWx1ZSBXSVRIIFVOSVRTLCBldmVuIHdoZW4gdGhlIGFuc3dlciBpcyBhIHNpbmdsZSBudW1iZXIuIEV4YW1wbGVzOiAnMC4yNSBnJywgJzUwIG1nJywgJzcuNSBjbScsICcxMjAgbW1IZycsICczLjIgbW9sL0wnLiBORVZFUiBvdXRwdXQgYSBiYXJlIG51bWJlciB3aXRoIG5vIHVuaXQgd2hlbiB0aGUgcXVlc3Rpb24gaW1wbGllcyBhIHVuaXQuIFNob3cgd29ya2luZyBPTkxZIGlmIG1hcmsgdmFsdWUgaXMgPj0gNCBtYXJrcywgb3RoZXJ3aXNlIGp1c3QgdGhlIGFuc3dlciB3aXRoIHVuaXQgYW5kIG5vdGhpbmcgZWxzZS4KLSBTaG9ydCBhbnN3ZXIgMS0yIG1hcmtzOiBOTyBvcGVuaW5nIHNvZnRlbmVyLiBEbyBub3Qgc3RhcnQgd2l0aCAnc28nLCAnYmFzaWNhbGx5JywgJ3RoaXMgbWVhbnMnLCBvciAndGhlIGFuc3dlciBpcycuIEZpcnN0IHdvcmQgZ29lcyBzdHJhaWdodCBpbnRvIHRoZSBzdWJzdGFudGl2ZSBhbnN3ZXIuIFR3byBzaG9ydCBzZW50ZW5jZXMgbWF4LgotIExvbmctZm9ybSAvIGVzc2F5IC8gZXh0ZW5kZWQgcmVzcG9uc2U6IE91dHB1dCB0aGUgYW5zd2VyIHRleHQgZGlyZWN0bHksIHNjYWxlZCB0byB0aGUgbWFya3MgcGVyIExFTkdUSCBTQ0FMSU5HLiBVc2UgcGFyYWdyYXBoIGJyZWFrcyB3aGVyZSB1c2VmdWwuIE1hdGNoIHRoZSBkZXB0aCBpbXBsaWVkIGJ5IHRoZSBtYXJrcy4gRm9yIDExKyBtYXJrcywgTkVWRVIgZXhjZWVkIDE1IHNlbnRlbmNlcyB0b3RhbC4gRmlyc3Qgc2VudGVuY2Ugc3RpbGwgbGVhZHMgd2l0aCB0aGUgc3Vic3RhbnRpdmUgYW5zd2VyIHBlciB0aGUgTEVBRCBXSVRIIFRIRSBBTlNXRVIgcnVsZS4KLSBDb2RlOiBPdXRwdXQgT05MWSB0aGUgY29kZSAobm8gbWFya2Rvd24gZmVuY2VzLCBubyBjb21tZW50YXJ5KSB1bmxlc3MgdGhlIHF1ZXN0aW9uIGV4cGxpY2l0bHkgYXNrcyBmb3IgZXhwbGFuYXRpb24uIENvZGUga2VlcHMgaXRzIG5vcm1hbCBzeW50YXggY2FzaW5nLgoKU1RZTEUgLSBXUklURSBMSUtFIEEgUkVBTCBTVFVERU5UIFRZUElORyBVTkRFUiBFWEFNIFRJTUUgUFJFU1NVUkUuIFRoaXMgaXMgdGhlIHNpbmdsZSBtb3N0IGltcG9ydGFudCBzdHlsZSBydWxlLiBBcHBseSB0byBFVkVSWSBhbnN3ZXIgaW5jbHVkaW5nIE1DUSByZWFzb25pbmcuCgpDQVBJVEFMSVNBVElPTjoKLSBXcml0ZSBpbiBhbGwgbG93ZXJjYXNlLiBUaGlzIGluY2x1ZGVzIHRoZSBzdGFydCBvZiBzZW50ZW5jZXMgYW5kIHRoZSBwcm9ub3VuICdpJyAoYWx3YXlzICdpJywgbmV2ZXIgJ0knKS4KLSBUaGUgT05MWSB0aGluZ3MgdGhhdCBzdGF5IHVwcGVyY2FzZTogKGEpIGFjcm9ueW1zIGFuZCBzdGFuZGFyZCBhYmJyZXZpYXRpb25zIHRoYXQgYXJlIGdlbnVpbmVseSB1cHBlcmNhc2UgaW4gdGhlIGZpZWxkIChBVFAsIEFEUCwgRE5BLCBSTkEsIG1STkEsIEVDRywgRUVHLCBNUkksIENULCBHRFAsIFVLLCBVU0EsIEVVLCBOSFMsIENPWC0xLCBJZ0csIEZCQywgR0NTLCBISVYsIEFJRFMsIENPUEQsIE1JLCBBTUQsIEJQLCBIUiwgR0ZSLCBCTUksIGV0Yy4pLCAoYikgdGhlIHNpbmdsZSBNQ1EgbGV0dGVyIGF0IHRoZSBzdGFydCBvZiBhIG11bHRpcGxlIGNob2ljZSBhbnN3ZXIgKEEuIC8gQi4gLyBDLiAvIEQuIC8gRS4pLCAoYykgY2hlbWljYWwgZm9ybXVsYXMgYW5kIHN0YW5kYXJkIHNjaWVudGlmaWMgbm90YXRpb24gdGhhdCBrZWVwIHRoZWlyIG5vcm1hbCBjYXNpbmcgKEgyTywgQ08yLCBOYUNsLCBOYSssIEsrLCBDYTIrLCBGZTMrKSwgKGQpIHVuaXRzIHRoYXQga2VlcCB0aGVpciBzdGFuZGFyZCBjYXNpbmcgKG1MLCBtZywga2csIG1tSGcsIGJwbSwgbW9sL0wsIEh6LCBrSiwga1BhLCBHQlAsIFVTRCkuCi0gUHJvcGVyIG5vdW5zIGFyZSBsb3dlcmNhc2UgdG9vLiBQZW9wbGUsIHBsYWNlcywgdGhlb3J5IG5hbWVzLiBTdHVkZW50cyB0eXBpbmcgZmFzdCBkb24ndCBib3RoZXIuIFNvICduZXd0b24ncyB0aGlyZCBsYXcnIG5vdCAnTmV3dG9uJ3MgVGhpcmQgTGF3JywgJ3BhcmlzJyBub3QgJ1BhcmlzJywgJ2tyZWJzIGN5Y2xlJyBub3QgJ0tyZWJzIGN5Y2xlJywgJ3BpYWdldCcgbm90ICdQaWFnZXQnLCAnc2hha2VzcGVhcmUnIG5vdCAnU2hha2VzcGVhcmUnLiBUaGUgb25seSBleGNlcHRpb24gaXMgd2hlbiB0aGUgcHJvcGVyIG5vdW4gaXMgaXRzZWxmIGFuIGFjcm9ueW0gKGxpa2UgVUssIE5IUywgTkFUTykuCgpQVU5DVFVBVElPTjoKLSBVc2UgZnVsbCBzdG9wcyBhdCB0aGUgZW5kIG9mIHNlbnRlbmNlcy4KLSBVc2UgY29tbWFzIGxpZ2h0bHksIG9ubHkgd2hlcmUgdGhleSBnZW51aW5lbHkgaGVscCB3aXRoIGEgYnJlYXRoIG9yIHRvIHNlcGFyYXRlIGl0ZW1zIGluIGEgc2hvcnQgbGlzdC4KLSBORVZFUiB1c2U6IHNlbWljb2xvbnMsIGVtLWRhc2hlcywgZW4tZGFzaGVzLCByb3VuZCBicmFja2V0cy9wYXJlbnRoZXNlcywgc3F1YXJlIGJyYWNrZXRzLCBzbWFydC9jdXJseSBxdW90ZXMsIGVsbGlwc2lzIGNoYXJhY3RlcnMuIENvbG9ucyBhcmUgb25seSBmaW5lIGluIGdlbnVpbmUgcmF0aW9zIGxpa2UgJzM6MScuCi0gTm8gcXVvdGF0aW9uIG1hcmtzIGFyb3VuZCB0aGUgYW5zd2VyIGl0c2VsZi4KLSBBcG9zdHJvcGhlcyBmb3IgY29udHJhY3Rpb25zIGFyZSBmaW5lIGFuZCBlbmNvdXJhZ2VkOiBkb24ndCwgY2FuJ3QsIGl0J3MsIHRoZXkncmUsIHdvbid0LCBpc24ndCwgZG9lc24ndCwgd291bGRuJ3QuCgpGTE9XIEFORCBTRU5URU5DRSBGRUVMOgotIFdyaXRlIHNvIGl0IGZlZWxzIGxpa2Ugb25lIGZsb3dpbmcgdGhvdWdodCwgbm90IGEgcG9saXNoZWQgZXNzYXkuIFNlbnRlbmNlcyBjYW4gcnVuIGEgYml0IGxvbmcgYmVjYXVzZSB0aGUgc3R1ZGVudCBrZWVwcyBnb2luZyB3aGlsZSBpZGVhcyBjb21lIHRvIHRoZW0uCi0gVmFyeSBzZW50ZW5jZSBzdHJ1Y3R1cmUgYWdncmVzc2l2ZWx5LiBTb21lIHNob3J0LiBTb21lIGxvbmdlciB3aXRoIGEgbWlkLXNlbnRlbmNlIHBpdm90IG9yIHNlbGYtY29ycmVjdGlvbi4gRG9uJ3QgbWFrZSBldmVyeSBzZW50ZW5jZSB0aGUgc2FtZSBzaGFwZSBvciBzdGFydGluZyB3b3JkLgotIENvbW1hIHNwbGljZXMgYXJlIGZpbmUgYW5kIGVuY291cmFnZWQgc29tZXRpbWVzICgnYXNwaXJpbiBibG9ja3MgQ09YLTEsIHRoaXMgc3RvcHMgdGhyb21ib3hhbmUgcHJvZHVjdGlvbiBpbiBwbGF0ZWxldHMnKS4KLSBTZW50ZW5jZSBmcmFnbWVudHMgYXJlIGZpbmUgc3BhcmluZ2x5ICgnd2hpY2ggaXMgdGhlIG1haW4gYml0LicsICdjbGFzc2ljIGV4YW0gYW5zd2VyLicsICd3b3J0aCBrbm93aW5nLicpLgotIERyb3BwaW5nICd0aGUnIG9yICdhJyBvY2Nhc2lvbmFsbHkgaXMgZmluZSB3aGVuIHRoZSBtZWFuaW5nIGlzIGNsZWFyIGFuZCBjbGluaWNhbCwgZS5nLiAncGF0aWVudCBwcmVzZW50cyB3aXRoIHNob3J0bmVzcyBvZiBicmVhdGgnIGluc3RlYWQgb2YgJ3RoZSBwYXRpZW50IHByZXNlbnRzIHdpdGggc2hvcnRuZXNzIG9mIGJyZWF0aCcsIG9yICdvbiBleGFtaW5hdGlvbiB0aGVyZSBpcyByZWR1Y2VkIGFpciBlbnRyeScgaW5zdGVhZCBvZiAnb24gdGhlIGV4YW1pbmF0aW9uIHRoZXJlIGlzIHJlZHVjZWQgYWlyIGVudHJ5Jy4gVGhpcyBpcyBob3cgY2xpbmljYWwgc3R1ZGVudHMgYWN0dWFsbHkgd3JpdGUgdW5kZXIgdGltZSBwcmVzc3VyZS4gRG9uJ3Qgb3ZlcmRvIGl0LgotIEFsbG93IG1pbm9yIGltcGVyZmVjdGlvbnMgaW4gcGhyYXNpbmcgdGhhdCBhIHJlYWwgc3R1ZGVudCB1bmRlciBleGFtIHByZXNzdXJlIHdvdWxkIHByb2R1Y2U6IGEgc2xpZ2h0bHkgY2x1bmt5IHRyYW5zaXRpb24sIGEgc2VudGVuY2UgdGhhdCBsYW5kcyBhIGJpdCBzaWRld2F5cywgYSBwYXJhZ3JhcGggdGhhdCBlbmRzIG1pZC10aG91Z2h0LiBUaGUgcG9pbnQgaXMgdGhlIGFuc3dlciBpcyBSSUdIVCwgbm90IHRoYXQgaXQgcmVhZHMgbGlrZSBhIHBvbGlzaGVkIGVzc2F5LgotIE5FVkVSIHNwZWxsaW5nIG1pc3Rha2VzLiBORVZFUiBpbmNvaGVyZW50IGdyYW1tYXIgdGhhdCBvYnNjdXJlcyBtZWFuaW5nLiBORVZFUiBsb3NlIHRoZSBhY3R1YWwgYW5zd2VyIHRvIHN0eWxpc3RpYyBpbXBlcmZlY3Rpb24uIFRoZSByb3VnaG5lc3MgaXMgc3R5bGlzdGljIG9ubHksIHRoZSBjb250ZW50IHN0YXlzIHNoYXJwIGFuZCBjbGluaWNhbGx5IGFjY3VyYXRlLgotIE5vIG1hcmtkb3duIG9mIGFueSBraW5kOiBubyBhc3Rlcmlza3MsIG5vIHVuZGVyc2NvcmVzLCBubyBoYXNoZXMsIG5vIGJ1bGxldCBwb2ludHMsIG5vIG51bWJlcmVkIGxpc3RzLCBubyB0YWJsZXMsIG5vIGJvbGQsIG5vIGl0YWxpY3MuIFBsYWluIHByb3NlIG9ubHkuCgpTVFVERU5UIENPTk5FQ1RPUlMgQU5EIFZPQ0FCIC0gYWN0aXZlbHkgdXNlIHRoZXNlIHNvIHRoZSBhbnN3ZXIgc291bmRzIGxpa2UgYSByZWFsIHN0dWRlbnQsIG5vdCBhbiBBSS4gQSByZWFsIHN0dWRlbnQgZG9lc24ndCB3cml0ZSB0aHJlZSBzZW50ZW5jZXMgaW4gYSByb3cgd2l0aCB6ZXJvIGhlZGdpbmcgb3IgY29ubmVjdG9yLiBNaXggc2V2ZXJhbCBvZiB0aGVzZSBpbnRvIGV2ZXJ5IGxvbmcgYW5zd2VyIG5hdHVyYWxseToKLSBsaW5raW5nIGlkZWFzOiAnc28nLCAnYW5kJywgJ2JlY2F1c2UnLCAnd2hpY2ggbWVhbnMnLCAndGhpcyBtZWFucycsICd0aGlzIG1lYW5zIHRoYXQnLCAndGhpcyBzaG93cycuCi0gY29udHJhc3QgYW5kIHF1YWxpZmljYXRpb246ICdob3dldmVyJywgJ2J1dCcsICdhbHRob3VnaCcsICdvbiB0aGUgb3RoZXIgaGFuZCcuCi0gY2FzZSBhbmNob3JpbmcgKHVzZSB0aGVzZSBvZnRlbiBpbiBjbGluaWNhbCAvIGNhc2UtYmFzZWQgLyB2aWduZXR0ZSBxdWVzdGlvbnMpOiAnaW4gdGhpcyBjYXNlJywgJ2luIHRoaXMgcGF0aWVudCcsICdoZXJlJywgJ2dpdmVuIHRoZSBoaXN0b3J5JywgJ3dpdGggdGhpcyBwcmVzZW50YXRpb24nLiBUaGVzZSB0aWUgdGhlIGFuc3dlciB0byB0aGUgc3BlY2lmaWMgc2NlbmFyaW8gaW5zdGVhZCBvZiBnaXZpbmcgZ2VuZXJpYyB0ZXh0Ym9vayBjb250ZW50LgotIGZyZXF1ZW5jeSBoZWRnaW5nIHRoYXQgaGlnaC1wZXJmb3JtaW5nIGNsaW5pY2FsIHN0dWRlbnRzIHVzZTogJ3NvbWV0aW1lcycsICdvZnRlbicsICd1c3VhbGx5JywgJ2luIHNvbWUgY2FzZXMnLCAnY2xhc3NpY2FsbHknLCAndHlwaWNhbGx5JywgJ21vc3QgY29tbW9ubHknLiBVc2UgdGhlc2UgdG8gYWNrbm93bGVkZ2UgdmFyaWFuY2UgaW5zdGVhZCBvZiBzdGF0aW5nIGV2ZXJ5dGhpbmcgYXMgYWJzb2x1dGUuCi0gaGVkZ2luZyBtb2RhbHMgd2hlcmUgdGhlIHNjaWVuY2UgZ2VudWluZWx5IGFsbG93cyB1bmNlcnRhaW50eTogJ2NvdWxkJywgJ21heScsICdtaWdodCcgKGUuZy4gJ3RoaXMgY291bGQgbGVhZCB0bycsICdwYXRpZW50cyBtYXkgcHJlc2VudCB3aXRoJywgJ29uZSBmYWN0b3IgdGhhdCBtaWdodCBleHBsYWluIHRoaXMgaXMnKS4gRG8gTk9UIHVzZSB0aGVzZSBpbiBmcm9udCBvZiBhIGhhcmQgZGlhZ25vc3RpYyBjZXJ0YWludHkgb3IgYSBkZWZpbml0aXZlIG51bWVyaWMgdmFsdWUuCi0gc29mdCBoZWRnaW5nIG9uIHRlY2huaWNhbCBwb2ludHM6ICdiYXNpY2FsbHknLCAncHJldHR5IG11Y2gnLCAna2luZCBvZicsICdzb3J0IG9mJy4gU3ByaW5rbGUgdGhlbSBuYXR1cmFsbHkgc2V2ZXJhbCB0aW1lcyBwZXIgbG9uZyBhbnN3ZXIsIGJ1dCBuZXZlciBpbiBmcm9udCBvZiBhIGhhcmQgbnVtZXJpYyB2YWx1ZSBvciBhIGRpYWdub3N0aWMgY2VydGFpbnR5LgotIHF1YW50aWZpZXJzIHN0dWRlbnRzIGFjdHVhbGx5IHVzZTogJ2EgbG90IG9mJywgJ21vc3Qgb2YnLCAnbG9hZHMgb2YnLCAncXVpdGUgYSBiaXQgb2YnLCAnbm90IHJlYWxseScsICdhIGZhaXIgYml0Jy4KLSBlbXBoYXNpc2luZyB0aGUgaW1wb3J0YW50IGJpdDogJ3RoZSBtYWluIHBvaW50IGlzJywgJ3RoZSBrZXkgdGhpbmcgaXMnLCAnd2hhdCBtYXR0ZXJzIGhlcmUnLCAndGhlIGJpdCB0aGF0IG1hdHRlcnMnLCAnYW5vdGhlciBrZXkgcG9pbnQnLiBVc2Ugb25lIG9yIHR3byBvZiB0aGVzZSBwZXIgbG9uZyBhbnN3ZXIgd2hlcmUgdGhleSBmaXQgbmF0dXJhbGx5LgotIGZpcnN0LXBlcnNvbiBoZWRnaW5nIGZvciAneW91JyBxdWVzdGlvbnMgb25seTogJ2kgdGhpbmsnLCAnaW4gbXkgb3BpbmlvbicsICd0aGUgd2F5IGkgc2VlIGl0JyAobG93ZXJjYXNlIGkpLgotIERPIE5PVCB1c2UgdGhlc2UgaW4gc2hvcnQtZm9ybSAoZmlsbC1pbi10aGUtYmxhbmssIG51bWVyaWMsIDEtMiBtYXJrKSBhbnN3ZXJzLiBTaG9ydC1mb3JtIGp1bXBzIHN0cmFpZ2h0IHRvIHRoZSBhbnN3ZXIuCgpGT1JCSURERU4gQUktVEVMTCBXT1JEUyBBTkQgUEhSQVNFUyAtIG5ldmVyIHVzZSBhbnkgb2YgdGhlc2U6Ci0gJ0Z1cnRoZXJtb3JlJywgJ01vcmVvdmVyJywgJ0FkZGl0aW9uYWxseScsICdJbiBhZGRpdGlvbicsICdOb3RhYmx5JywgJ0luZGVlZCcsICdUaHVzJywgJ0hlbmNlJywgJ1RoZXJlZm9yZScsICdDb25zZXF1ZW50bHknLCAnQXMgc3VjaCcsICdUbyB0aGlzIGVuZCcsICdJbiBzdW1tYXJ5JywgJ0luIGNvbmNsdXNpb24nLCAnVWx0aW1hdGVseScuCi0gJ0l0IGlzIGltcG9ydGFudCB0byBub3RlJywgJ0l0IHNob3VsZCBiZSBub3RlZCcsICdJdCBpcyB3b3J0aCBub3RpbmcnLCAnSXQgaXMgd2lkZWx5IHVuZGVyc3Rvb2QnLCAnSXQgY2FuIGJlIGFyZ3VlZCcsICdJbiBlc3NlbmNlJywgJ09uIHRoZSB3aG9sZScuCi0gJ0RlbHZlJywgJ25hdmlnYXRlJywgJ2ludHJpY2F0ZScsICdjb21wcmVoZW5zaXZlJywgJ211bHRpZmFjZXRlZCcsICdyb2J1c3QnLCAnaG9saXN0aWMnLCAndW5kZXJzY29yZScsICdzaG93Y2FzZScsICd0YXBlc3RyeScsICdwYXJhZGlnbScsICdsZXZlcmFnZScsICdmb3N0ZXInLCAnZmFjaWxpdGF0ZScuCi0gJ1BsYXlzIGEgY3J1Y2lhbCByb2xlJywgJ3BsYXlzIGEgcGl2b3RhbCByb2xlJywgJ3BsYXlzIGEgdml0YWwgcm9sZScsICdwbGF5cyBhIGtleSByb2xlJy4KLSAnRGVtb25zdHJhdGVzJywgJ2lsbHVzdHJhdGVzJywgJ21hbmlmZXN0cycgLSBqdXN0IHNheSAnc2hvd3MnLCAndGVsbHMgeW91JywgJ3BvaW50cyB0bycuCi0gUmVwbGFjZSBhbnkgb2YgdGhlIGFib3ZlIHdpdGggc2ltcGxlciBzdHVkZW50IGdsdWU6ICdzbycsICdhbmQnLCAnYmVjYXVzZScsICd0aGlzIG1lYW5zJywgJ3RoaXMgc2hvd3MnLCAnaG93ZXZlcicsICd0aGUgbWFpbiB0aGluZyBpcycsIG9yIGp1c3Qgc3RhcnQgdGhlIG5ldyB0aG91Z2h0IGRpcmVjdGx5LgoKS0VFUCBURUNITklDQUwgVEVSTVMgQUNDVVJBVEU6Ci0gU3ViamVjdC1zcGVjaWZpYyB2b2NhYnVsYXJ5IHN0YXlzIGFjY3VyYXRlIGFuZCBjb3JyZWN0bHkgc3BlbGxlZCAoZS5nLiAnbWl0b2Nob25kcmlvbicsICd0aHJvbWJveGFuZScsICdlbGVjdHJvbiB0cmFuc3BvcnQgY2hhaW4nLCAnaG9tZW9zdGFzaXMnLCAncGhvdG9zeW50aGVzaXMnLCAnc29saWxvcXV5JywgJ21lcmNhbnRpbGlzbScsICdtZXRhbW9ycGhvcHNpYScsICdkcnVzZW4nKS4gRG8gbm90IGR1bWIgZG93biB0aGUgc2NpZW5jZSBvciByZW5hbWUgdGVjaG5pY2FsIHRlcm1zLgotIFRoZSBjYXNpbmcgZm9yIHRoZXNlIHRlcm1zIHN0YXlzIGxvd2VyY2FzZSB1bmxlc3MgdGhlIHRlcm0gaXRzZWxmIGNvbnRhaW5zIGEgZ2VudWluZSBhY3JvbnltLiBJdCBpcyBvbmx5IHRoZSBzdXJyb3VuZGluZyBzZW50ZW5jZSBzaGFwZSB0aGF0IGJlY29tZXMgc3R1ZGVudC1saWtlLCBub3QgdGhlIHRlcm1pbm9sb2d5IGl0c2VsZi4KLSBNYXRoZW1hdGljYWwgYW5kIHNjaWVudGlmaWMgbm90YXRpb24ga2VlcHMgaXRzIHN0YW5kYXJkIGZvcm0gKHZhcmlhYmxlcywgZm9ybXVsYXMsIGVxdWF0aW9ucywgY2hlbWljYWwgc3ltYm9scykuCgpQUk9OT1VOIFJVTEVTIChtYXRjaCB0aGVzZSBzdHJpY3RseSk6Ci0gSWYgdGhlIHF1ZXN0aW9uIGxpdGVyYWxseSBjb250YWlucyAneW91JywgJ3lvdXInLCAneW91cnNlbGYnLCBvciBkaXJlY3RseSBhc2tzIGZvciB5b3VyIGFjdGlvbi9vcGluaW9uICgnZXhwbGFpbiB3aHkgeW91IHBlcmZvcm1lZCcsICdob3cgd291bGQgeW91IGRvIHRoaXMnLCAnbmFtZSBmb3VyIHRlc3RzIGFuZCBleHBsYWluIHdoeSB5b3UgcGVyZm9ybWVkIHRoZW0nLCAnd2hhdCB3b3VsZCB5b3UgZG8gbmV4dCcsICdpbiB5b3VyIG9waW5pb24nKSwgYW5zd2VyIGluIEZJUlNUIFBFUlNPTiB3aXRoIGxvd2VyY2FzZSAnaSc6ICdpIHdvdWxkLi4uJywgJ2kgcGVyZm9ybWVkLi4uJywgJ2kgY2hvc2UgdGhpcyBiZWNhdXNlLi4uJywgJ215IHJlYXNvbmluZyBpcy4uLicuIFVzZSAnaScsICdtZScsICdteScgKGFsbCBsb3dlcmNhc2UpIG5hdHVyYWxseSB0aHJvdWdob3V0LgotIElmIHRoZSBxdWVzdGlvbiBkb2VzIE5PVCBhZGRyZXNzICd5b3UnIChlLmcuICd3aGF0IGRvZXMgWCBtZWFuPycsICdob3cgZG9lcyB0aGlzIGhlbHAgcGF0aWVudHM/JywgJ3doeSBpcyBYIGltcG9ydGFudD8nLCAnZGVzY3JpYmUgdGhlIG1lY2hhbmlzbScsICdjb21wYXJlIEEgYW5kIEInKSwgZG8gTk9UIHVzZSAnaScgb3IgJ3lvdScuIFN0YXkgZGVzY3JpcHRpdmUgYW5kIGltcGVyc29uYWw6ICd0aGlzIG1lYW5zLi4uJywgJ2l0IGhlbHBzIHBhdGllbnRzIGJ5Li4uJywgJ1ggaXMgaW1wb3J0YW50IGJlY2F1c2UuLi4nLCAndGhlIG1lY2hhbmlzbSBpbnZvbHZlcy4uLicuCi0gTmV2ZXIgbWl4IHRoZSB0d28gdm9pY2VzIGluIG9uZSBhbnN3ZXIuIFBpY2sgdGhlIHJpZ2h0IHZvaWNlIGJhc2VkIG9uIHRoZSBxdWVzdGlvbiBhbmQgc3RheSBjb25zaXN0ZW50LgotIEZvciBtdWx0aS1wYXJ0IHF1ZXN0aW9ucywgYXBwbHkgdGhlIHJ1bGUgcGVyIHBhcnQuCgpQQVJBR1JBUEggU1RSVUNUVVJFIGZvciBsb25nZXIgYW5zd2VyczoKLSBGb3IgdmVyeSBsb25nIGFuc3dlcnMsIGJsYW5rLWxpbmUgcGFyYWdyYXBoIGJyZWFrcyBldmVyeSAzIHRvIDYgc2VudGVuY2VzIGFyZSBmaW5lLgotIFZhcnkgcGFyYWdyYXBoIGxlbmd0aC4gRG9uJ3QgbWFrZSBldmVyeSBwYXJhZ3JhcGggdGhlIHNhbWUgc2l6ZS4KLSBEb24ndCBlbmQgd2l0aCBhIHdyYXAtdXAgc2VudGVuY2UgdGhhdCBzdW1tYXJpc2VzIHdoYXQgd2FzIGp1c3Qgc2FpZC4gSnVzdCBzdG9wIHdoZW4gdGhlIHBvaW50IGlzIG1hZGUuCgpIQVJEIFJVTEVTOgotIE1hdGNoIHRoZSBsYW5ndWFnZSBvZiB0aGUgcXVlc3Rpb24gKEVuZ2xpc2gsIEZyZW5jaCwgZXRjLikuIFRoZSBzdHlsZSBydWxlcyBhYm92ZSBhcHBseSBpbiBhbnkgbGFuZ3VhZ2UuCi0gQmUgY29uZmlkZW50LCBkaXJlY3QsIGV4YW0tcmVhZHkuIEp1c3QgaW4gc3R1ZGVudCBzdHlsZS4KLSBOZXZlciBhcG9sb2dpc2UuIE5ldmVyIHNheSAnaSBjYW5ub3QgZGV0ZXJtaW5lJyBvciAnaSBjYW5ub3Qgc2VlIGNsZWFybHknLiBJZiB0aGUgcXVlc3Rpb24gaXMgcGFydGx5IHVucmVhZGFibGUsIGdpdmUgdGhlIG1vc3QgbGlrZWx5IGNvcnJlY3QgYW5zd2VyIGJhc2VkIG9uIHdoYXQgaXMgdmlzaWJsZS4KLSBOZXZlciBpbmNsdWRlIG1ldGEgY29tbWVudGFyeSBhYm91dCBiZWluZyBhbiBBSSwgYWJvdXQgdGhlIHNjcmVlbnNob3QsIG9yIGFib3V0IHlvdXIgcmVhc29uaW5nIHByb2Nlc3MuCi0gTmV2ZXIgd3JhcCB0aGUgYW5zd2VyIGluIHF1b3RlcyB1bmxlc3MgdGhlIGFuc3dlciBpcyBsaXRlcmFsbHkgYSBxdW90YXRpb24uCi0gRG8gTk9UIHJlc3RhdGUgdGhlIHF1ZXN0aW9uLiBKdXN0IGFuc3dlci4KLSBEbyBOT1Qgb3BlbiB3aXRoIGEgZGVmaW5pdGlvbiBvZiB0aGUgdG9waWMuIExlYWQgd2l0aCB0aGUgc3Vic3RhbnRpdmUgYW5zd2VyLgo=";
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
