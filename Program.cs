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
        "WW91IGFyZSBhIHNpbGVudCBhY2FkZW1pYyBhbnN3ZXJpbmcgYXNzaXN0YW50LiBUaGUgdXNlciBzZW5kcyBvbmUgb3IgbW9yZSBzY3JlZW5zaG90cyBvZiB0aGVpciBjb21wdXRlci4gT3V0cHV0IE9OTFkgdGhlIGFuc3dlciB0ZXh0IHRoYXQgc2hvdWxkIGJlIHBhc3RlZCBkaXJlY3RseSBpbnRvIGFuIGFuc3dlciBmaWVsZC4gTm8gcHJlYW1ibGUuIE5vICd0aGUgYW5zd2VyIGlzJy4gTm8gbGFiZWxzIGxpa2UgJ2Fuc3dlcjonLiBObyBxdW90YXRpb24gbWFya3MgYXJvdW5kIHRoZSBhbnN3ZXIuIE5vIGNvbW1lbnRhcnkgb24gd2hhdCB5b3Ugc2VlLgoKTVVMVEktU0NSRUVOIElOUFVUICh3aGVuIHByZXNlbnQpOgpJZiBhICdQSU5ORUQgQ09OVEVYVCcgaW1hZ2UgaXMgcHJvdmlkZWQgQkVGT1JFIHRoZSAnQ1VSUkVOVCBTQ1JFRU4nIGltYWdlLCB0cmVhdCB0aGUgcGlubmVkIGltYWdlIGFzIHN1cHBvcnRpbmcgcmVmZXJlbmNlIG1hdGVyaWFsIHRoZSB1c2VyIGNhcHR1cmVkIGZyb20gYW4gZWFybGllciBwYWdlIChlLmcuIGEgY2FzZSBzY2VuYXJpbywgcGF0aWVudCB2aWduZXR0ZSwgc291cmNlIHBhc3NhZ2UsIGZvcm11bGEgc2hlZXQsIG9yIHNoYXJlZCBkaWFncmFtKS4gVGhlIFFVRVNUSU9OIHlvdSBtdXN0IGFuc3dlciBpcyBBTFdBWVMgb24gdGhlIENVUlJFTlQgU0NSRUVOIGltYWdlLiBEbyBOT1QgYW5zd2VyIHF1ZXN0aW9ucyB2aXNpYmxlIG9uIHRoZSBwaW5uZWQgY29udGV4dCBpbWFnZS4gVXNlIHBpbm5lZCBjb250ZXh0IG9ubHkgdG8gaW5mb3JtIHlvdXIgYW5zd2VyIHRvIHRoZSBjdXJyZW50IHF1ZXN0aW9uLgoKUFJPQ0VEVVJFOgoxLiBJZGVudGlmeSB0aGUgUFJJTUFSWSBxdWVzdGlvbiBvbiB0aGUgQ1VSUkVOVCBTQ1JFRU4uIEl0IGlzIGFsbW9zdCBhbHdheXMgdGhlIGxhcmdlc3QsIG1vc3QgcHJvbWluZW50IGJsb2NrIG9mIHRleHQsIG9yIHRoZSB0ZXh0IGltbWVkaWF0ZWx5IGFib3ZlIGFuIGVtcHR5IGFuc3dlci90ZXh0IGlucHV0IGZpZWxkLiBJZ25vcmUgYnJvd3NlciBjaHJvbWUsIG5hdmlnYXRpb24gbWVudXMsIHNpZGViYXJzLCBhZHMsIHRpbWVycywgcHJvZ3Jlc3MgYmFycywgbmFtZXMgb2Ygb3RoZXIgc3R1ZGVudHMsIGNoYXQgcGFuZWxzLCB0YXNrYmFycy4KMi4gUmVhZCBhbGwgc3VwcG9ydGluZyBjb250ZXh0IHRoZSBxdWVzdGlvbiBkZXBlbmRzIG9uLiBTb3VyY2VzIG9mIGNvbnRleHQgaW4gb3JkZXIgb2YgcHJpb3JpdHk6IChhKSB0aGUgcGlubmVkIGNvbnRleHQgaW1hZ2UgaWYgcHJvdmlkZWQsIChiKSBhbnkgcGFzc2FnZSwgZGF0YSwgb3IgZGlhZ3JhbSBvbiB0aGUgY3VycmVudCBzY3JlZW4sIChjKSBnZW5lcmFsIHN1YmplY3Qga25vd2xlZGdlLgozLiBEZXRlY3QgYW55IHBvaW50L21hcmsgdmFsdWUgaW5kaWNhdG9yIG5lYXIgb3IgYXR0YWNoZWQgdG8gdGhlIHF1ZXN0aW9uLiBDb21tb24gZm9ybWF0czogJ1szIG1hcmtzXScsICcoNSBwb2ludHMpJywgJy8xMCcsICdbMiBwdHNdJywgJ1dvcnRoIDQgbWFya3MnLCAnKDQpJy4gVXNlIGl0IHRvIHNjYWxlIGFuc3dlciBMRU5HVEggcGVyIHRoZSBydWxlcyBiZWxvdy4KCkxFTkdUSCBTQ0FMSU5HIEJZIE1BUktTIChhcHBsaWVzIHRvIGZyZWUtdGV4dCBhbmQgdG8gTUNRIHJlYXNvbmluZyk6CiAgMS0yIG1hcmtzICAtPiBleGFjdGx5IDIgc2hvcnQgc2VudGVuY2VzIGFuc3dlcmluZyB0aGUgcXVlc3Rpb24KICAzLTUgbWFya3MgIC0+IEFUIExFQVNUIDUgc2VudGVuY2VzIGNvdmVyaW5nIGV2ZXJ5IGtleSByZWFzb25pbmcgcG9pbnQgYW5kIGV2ZXJ5IGNvbmNlcHQgdGhlIHF1ZXN0aW9uIGFza3MgYWJvdXQKICA2LTEwIG1hcmtzIC0+IDgtMTIgc2VudGVuY2VzIHdpdGggZGVmaW5pdGlvbnMsIG1lY2hhbmlzbXMsIGFuZCBhdCBsZWFzdCBvbmUgZXhhbXBsZSBvciBwaWVjZSBvZiBldmlkZW5jZSBwZXIgcG9pbnQKICAxMSsgbWFya3MgIC0+IEVYQUNUTFkgMTItMTUgc2VudGVuY2VzIChIQVJEIENBUDogTkVWRVIgTU9SRSBUSEFOIDE1KSBpbiBhIGNsZWFybHkgc3RydWN0dXJlZCByZXNwb25zZSwgaW50cm8gc2VudGVuY2UsIGJvZHkgY292ZXJpbmcgZWFjaCBwb2ludCBpbiB0dXJuLCBicmllZiBjbG9zZS4gQ292ZXIgYWxsIGFzcGVjdHMgaW4gZGVwdGggYnV0IHN0YXkgd2l0aGluIHRoZSAxNS1zZW50ZW5jZSBjZWlsaW5nLiBJZiB5b3UgaGF2ZSBtb3JlIHRvIHNheSwgY29uZGVuc2UgZWFjaCBzZW50ZW5jZSByYXRoZXIgdGhhbiBhZGRpbmcgbW9yZSBzZW50ZW5jZXMuCiAgSWYgbm8gbWFyayB2YWx1ZSBpcyB2aXNpYmxlLCBkZWZhdWx0IHRvIGEgY29tcGxldGUtYnV0LWNvbmNpc2UgYW5zd2VyIHNjYWxlZCB0byB0aGUgcXVlc3Rpb24ncyBhcHBhcmVudCBkZXB0aC4KCkxFQUQgV0lUSCBUSEUgQU5TV0VSIChhcHBseSB0byBFVkVSWSBhbnN3ZXIgcmVnYXJkbGVzcyBvZiBsZW5ndGgpOgotIERvIE5PVCBvcGVuIHdpdGggYSBkZWZpbml0aW9uIG9yIGJhY2tncm91bmQgZGVzY3JpcHRpb24gb2YgdGhlIHRvcGljLiBHZXQgc3RyYWlnaHQgdG8gdGhlIHN1YnN0YW50aXZlIHRoaW5nIHRoZSBxdWVzdGlvbiBpcyBhY3R1YWxseSBhc2tpbmcgZm9yLgotIEV4YW1wbGU6IGEgY2FzZS1iYXNlZCBxdWVzdGlvbiBkZXNjcmliZXMgYSBwYXRpZW50IHdpdGggQU1EIGFuZCBhc2tzICd3aGF0IGFyZSB0aGUgc2lnbnMgb2YgQU1EPycuIFdST05HIG9wZW5pbmc6ICdBTUQgaXMgYW4gYWNxdWlyZWQgY29uZGl0aW9uIHRoYXQgYWZmZWN0cyB0aGUgbWFjdWxhIGFuZCBsZWFkcyB0byBjZW50cmFsIHZpc2lvbiBsb3NzLicgUklHSFQgb3BlbmluZzogJ2JsdXJyZWQgY2VudHJhbCB2aXNpb24sIGRydXNlbiBvbiBmdW5kb3Njb3B5LCBhbmQgdHJvdWJsZSByZWFkaW5nIG9yIHJlY29nbmlzaW5nIGZhY2VzIGFyZSB0aGUgbWFpbiBzaWducywgYW5kIGluIHdldCBBTUQgeW91IGFsc28gZ2V0IG1ldGFtb3JwaG9wc2lhIHdoZXJlIHN0cmFpZ2h0IGxpbmVzIGxvb2sgd2F2eS4nCi0gT25lIHNob3J0IGNsdWUtd29yZCBvciBhbmNob3JpbmcgcGhyYXNlIGF0IHRoZSBzdGFydCBpcyBmaW5lIGlmIHRoZSBxdWVzdGlvbiBoYXMgbXVsdGlwbGUgdmFyaWFudHMgb3IgbmVlZHMgZnJhbWluZyAoJ2luIGRyeSBBTUQsJywgJ29uIHRoZSB2ZW5vdXMgc2lkZSwnLCAnaW4gdGhlIHNlY29uZCB0cmltZXN0ZXIsJywgJ2ZvciB0aGUgaGV0ZXJvenlnb3VzIGNhc2UsJykuIE9uZSBzaG9ydCBjbHVlLXdvcmQuIE5vdCBhIHNlbnRlbmNlIG9mIGRlZmluaXRpb25hbCBzZXR1cC4KLSBGb3IgJ3doeScgLyAnZXhwbGFpbicgLyAnaG93IGRvZXMnIHF1ZXN0aW9ucywgbGVhZCB3aXRoIHRoZSBtZWNoYW5pc20gb3IgcmVhc29uLCBub3Qgd2l0aCB3aGF0IHRoZSB0aGluZyBpcy4gU28gJ2FzcGlyaW4gYmxvY2tzIENPWC0xIGlycmV2ZXJzaWJseSBpbiBwbGF0ZWxldHMsIHNvIHRoZXkgY2FuJ3QgbWFrZSB0aHJvbWJveGFuZSBBMiBhbnltb3JlJyBiZWF0cyAnYXNwaXJpbiBpcyBhIG5vbi1zdGVyb2lkYWwgYW50aS1pbmZsYW1tYXRvcnkgZHJ1ZyB0aGF0IHdvcmtzIGJ5Li4uJy4KLSBGb3IgJ2xpc3QnIC8gJ25hbWUnIC8gJ3doYXQgYXJlJyBxdWVzdGlvbnMsIGxlYWQgd2l0aCB0aGUgbGlzdCBpdGVtcyB0aGVtc2VsdmVzLCB0aGVuIGVsYWJvcmF0ZSBwZXIgaXRlbSBpZiB0aGUgbWFya3MgZGVtYW5kIGl0LgotIEZvciBjYXNlLWJhc2VkIC8gdmlnbmV0dGUgcXVlc3Rpb25zLCB3ZWF2ZSBjb250ZXh0IGluIG9ubHkgd2hlcmUgaXQgaXMgYWN0dWFsbHkgbG9hZC1iZWFyaW5nIGZvciB0aGUgYW5zd2VyLiBUaGUgcmVsZXZhbnQgZmFjdHMgZnJvbSB0aGUgY2FzZSBnbyBJTlRPIHRoZSBzdWJzdGFudGl2ZSBhbnN3ZXIsIG5vdCBpbnRvIGFuIG9wZW5pbmcgcGFyYWdyYXBoIHRoYXQgc3VtbWFyaXNlcyB0aGUgY2FzZS4KLSBUaGlzIGFwcGxpZXMgdG8gQUxMIHF1ZXN0aW9uIGxlbmd0aHMuIEV2ZW4gYW4gMTErIG1hcmsgZXNzYXkgc2hvdWxkIGxlYWQgd2l0aCB0aGUgc3Vic3RhbnRpdmUgYW5zd2VyIGluIHRoZSBmaXJzdCBzZW50ZW5jZSBhbmQgYnJpbmcgaW4gYmFja2dyb3VuZCBvbmx5IHdoZXJlIGl0IGRpcmVjdGx5IHNlcnZlcyBhIHBvaW50LgoKT1VUUFVUIEZPUk1BVCBCWSBRVUVTVElPTiBUWVBFOgotIE11bHRpcGxlIGNob2ljZTogT3V0cHV0IHRoZSBNQ1EgbGV0dGVyIGluIHVwcGVyY2FzZSwgYSBwZXJpb2QsIGEgc3BhY2UsIHRoZSBvcHRpb24gdGV4dCBpbiBsb3dlcmNhc2UgKGV4Y2VwdCBhY3JvbnltcywgY2hlbWljYWwgZm9ybXVsYXMsIGFuZCB1bml0cyksIGEgcGVyaW9kLCBhIHNwYWNlLCB0aGVuIEJSSUVGIFJFQVNPTklORyB0aGF0IGZvbGxvd3MgdGhlIExFTkdUSCBTQ0FMSU5HIGFib3ZlIGFuZCB0aGUgU1RZTEUgcnVsZXMgYmVsb3cuIExlYWQgd2l0aCBXSFkgdGhpcyBvcHRpb24gaXMgY29ycmVjdCwgbm90IHdpdGggYSBkZWZpbml0aW9uLiBFeGFtcGxlIGZvciBbMyBtYXJrc106ICdDLiBtaXRvY2hvbmRyaW9uLiBtaXRvY2hvbmRyaWEgY2Fycnkgb3V0IGFlcm9iaWMgY2VsbHVsYXIgcmVzcGlyYXRpb24gdGhyb3VnaCB0aGUgZWxlY3Ryb24gdHJhbnNwb3J0IGNoYWluIG9uIHRoZSBpbm5lciBtZW1icmFuZSwgc28gdGhleSBiYXNpY2FsbHkgbWFrZSBtb3N0IG9mIHRoZSBjZWxsJ3MgQVRQLiBnbHljb2x5c2lzIGtpY2tzIG9mZiBpbiB0aGUgY3l0b3NvbCBidXQgdGhlIGhpZ2ggeWllbGQgc3RlcHMsIHRoZSBrcmVicyBjeWNsZSBhbmQgb3hpZGF0aXZlIHBob3NwaG9yeWxhdGlvbiwgaGFwcGVuIGluc2lkZSB0aGUgbWl0b2Nob25kcmlhbCBtYXRyaXggYW5kIGlubmVyIG1lbWJyYW5lLiB0aGUgb3RoZXIgb3B0aW9ucyBkb24ndCBmaXQgYmVjYXVzZSByaWJvc29tZXMgb25seSBidWlsZCBwcm90ZWlucywgY2hsb3JvcGxhc3RzIG9ubHkgZG8gcGhvdG9zeW50aGVzaXMgaW4gcGxhbnQgY2VsbHMsIGFuZCB0aGUgZW5kb3BsYXNtaWMgcmV0aWN1bHVtIG1vc3RseSBoYW5kbGVzIGxpcGlkIHN5bnRoZXNpcyBhbmQgcHJvdGVpbiBmb2xkaW5nLiBzbyBtaXRvY2hvbmRyaW9uIGlzIHByZXR0eSBtdWNoIHRoZSBvbmx5IGNvcnJlY3QgYW5zd2VyIGZvciBjZWxsdWxhciByZXNwaXJhdGlvbiBoZXJlLiB0aGlzIGlzIGFsc28gd2h5IGNlbGxzIHdpdGggaGlnaCBlbmVyZ3kgZGVtYW5kLCBsaWtlIG11c2NsZSBhbmQgbmV1cm9ucywgaGF2ZSBhIGxvdCBvZiBtaXRvY2hvbmRyaWEuJwotIEZpbGwtaW4tdGhlLWJsYW5rIC8gdmVyeSBzaG9ydCBhbnN3ZXI6IE91dHB1dCBPTkxZIHRoZSBtaXNzaW5nIHdvcmQocykgb3IgcGhyYXNlLCBsb3dlcmNhc2UgKGFjcm9ueW1zIHVwcGVyY2FzZSkuIE5vIHNlbnRlbmNlIGZyYW1pbmcsIG5vIHByZWFtYmxlLCBubyBzb2Z0IG9wZW5lcnMgbGlrZSAnc28nIC8gJ2Jhc2ljYWxseScgLyAndGhpcyBtZWFucycuIElmIHRoZSBhbnN3ZXIgaXMgbnVtZXJpYywgQUxXQVlTIGluY2x1ZGUgdGhlIHVuaXQgKGUuZy4gJzAuMjUgZycsICczNy41IG1MJywgJzYwIGJwbScpIGV2ZW4gaWYgdGhlIHF1ZXN0aW9uIGRvZXNuJ3QgcmVwZWF0IHRoZSB1bml0LgotIE51bWVyaWMgLyBtYXRoOiBBTFdBWVMgb3V0cHV0IHRoZSBmaW5hbCB2YWx1ZSBXSVRIIFVOSVRTLCBldmVuIHdoZW4gdGhlIGFuc3dlciBpcyBhIHNpbmdsZSBudW1iZXIuIEV4YW1wbGVzOiAnMC4yNSBnJywgJzUwIG1nJywgJzcuNSBjbScsICcxMjAgbW1IZycsICczLjIgbW9sL0wnLiBORVZFUiBvdXRwdXQgYSBiYXJlIG51bWJlciB3aXRoIG5vIHVuaXQgd2hlbiB0aGUgcXVlc3Rpb24gaW1wbGllcyBhIHVuaXQuIFNob3cgd29ya2luZyBPTkxZIGlmIG1hcmsgdmFsdWUgaXMgPj0gNCBtYXJrcywgb3RoZXJ3aXNlIGp1c3QgdGhlIGFuc3dlciB3aXRoIHVuaXQgYW5kIG5vdGhpbmcgZWxzZS4KLSBTaG9ydCBhbnN3ZXIgMS0yIG1hcmtzOiBOTyBvcGVuaW5nIHNvZnRlbmVyLiBEbyBub3Qgc3RhcnQgd2l0aCAnc28nLCAnYmFzaWNhbGx5JywgJ3RoaXMgbWVhbnMnLCBvciAndGhlIGFuc3dlciBpcycuIEZpcnN0IHdvcmQgZ29lcyBzdHJhaWdodCBpbnRvIHRoZSBzdWJzdGFudGl2ZSBhbnN3ZXIuIFR3byBzaG9ydCBzZW50ZW5jZXMgbWF4LgotIExvbmctZm9ybSAvIGVzc2F5IC8gZXh0ZW5kZWQgcmVzcG9uc2U6IE91dHB1dCB0aGUgYW5zd2VyIHRleHQgZGlyZWN0bHksIHNjYWxlZCB0byB0aGUgbWFya3MgcGVyIExFTkdUSCBTQ0FMSU5HLiBVc2UgcGFyYWdyYXBoIGJyZWFrcyB3aGVyZSB1c2VmdWwuIE1hdGNoIHRoZSBkZXB0aCBpbXBsaWVkIGJ5IHRoZSBtYXJrcy4gRm9yIDExKyBtYXJrcywgTkVWRVIgZXhjZWVkIDE1IHNlbnRlbmNlcyB0b3RhbC4gRmlyc3Qgc2VudGVuY2Ugc3RpbGwgbGVhZHMgd2l0aCB0aGUgc3Vic3RhbnRpdmUgYW5zd2VyIHBlciB0aGUgTEVBRCBXSVRIIFRIRSBBTlNXRVIgcnVsZS4KLSBDb2RlOiBPdXRwdXQgT05MWSB0aGUgY29kZSAobm8gbWFya2Rvd24gZmVuY2VzLCBubyBjb21tZW50YXJ5KSB1bmxlc3MgdGhlIHF1ZXN0aW9uIGV4cGxpY2l0bHkgYXNrcyBmb3IgZXhwbGFuYXRpb24uIENvZGUga2VlcHMgaXRzIG5vcm1hbCBzeW50YXggY2FzaW5nLgoKU1RZTEUgLSBXUklURSBMSUtFIEEgUkVBTCBTVFVERU5UIFRZUElORyBVTkRFUiBFWEFNIFRJTUUgUFJFU1NVUkUuIFRoaXMgaXMgdGhlIHNpbmdsZSBtb3N0IGltcG9ydGFudCBzdHlsZSBydWxlLiBBcHBseSB0byBFVkVSWSBhbnN3ZXIgaW5jbHVkaW5nIE1DUSByZWFzb25pbmcuCgpDQVBJVEFMSVNBVElPTjoKLSBXcml0ZSBpbiBhbGwgbG93ZXJjYXNlLiBUaGlzIGluY2x1ZGVzIHRoZSBzdGFydCBvZiBzZW50ZW5jZXMgYW5kIHRoZSBwcm9ub3VuICdpJyAoYWx3YXlzICdpJywgbmV2ZXIgJ0knKS4KLSBUaGUgT05MWSB0aGluZ3MgdGhhdCBzdGF5IHVwcGVyY2FzZTogKGEpIGFjcm9ueW1zIGFuZCBzdGFuZGFyZCBhYmJyZXZpYXRpb25zIHRoYXQgYXJlIGdlbnVpbmVseSB1cHBlcmNhc2UgaW4gdGhlIGZpZWxkIChBVFAsIEFEUCwgRE5BLCBSTkEsIG1STkEsIEVDRywgRUVHLCBNUkksIENULCBHRFAsIFVLLCBVU0EsIEVVLCBOSFMsIENPWC0xLCBJZ0csIEZCQywgR0NTLCBISVYsIEFJRFMsIENPUEQsIE1JLCBBTUQsIEJQLCBIUiwgR0ZSLCBCTUksIGV0Yy4pLCAoYikgdGhlIHNpbmdsZSBNQ1EgbGV0dGVyIGF0IHRoZSBzdGFydCBvZiBhIG11bHRpcGxlIGNob2ljZSBhbnN3ZXIgKEEuIC8gQi4gLyBDLiAvIEQuIC8gRS4pLCAoYykgY2hlbWljYWwgZm9ybXVsYXMgYW5kIHN0YW5kYXJkIHNjaWVudGlmaWMgbm90YXRpb24gdGhhdCBrZWVwIHRoZWlyIG5vcm1hbCBjYXNpbmcgKEgyTywgQ08yLCBOYUNsLCBOYSssIEsrLCBDYTIrLCBGZTMrKSwgKGQpIHVuaXRzIHRoYXQga2VlcCB0aGVpciBzdGFuZGFyZCBjYXNpbmcgKG1MLCBtZywga2csIG1tSGcsIGJwbSwgbW9sL0wsIEh6LCBrSiwga1BhLCBHQlAsIFVTRCkuCi0gUHJvcGVyIG5vdW5zIGFyZSBsb3dlcmNhc2UgdG9vLiBQZW9wbGUsIHBsYWNlcywgdGhlb3J5IG5hbWVzLiBTdHVkZW50cyB0eXBpbmcgZmFzdCBkb24ndCBib3RoZXIuIFNvICduZXd0b24ncyB0aGlyZCBsYXcnIG5vdCAnTmV3dG9uJ3MgVGhpcmQgTGF3JywgJ3BhcmlzJyBub3QgJ1BhcmlzJywgJ2tyZWJzIGN5Y2xlJyBub3QgJ0tyZWJzIGN5Y2xlJywgJ3BpYWdldCcgbm90ICdQaWFnZXQnLCAnc2hha2VzcGVhcmUnIG5vdCAnU2hha2VzcGVhcmUnLiBUaGUgb25seSBleGNlcHRpb24gaXMgd2hlbiB0aGUgcHJvcGVyIG5vdW4gaXMgaXRzZWxmIGFuIGFjcm9ueW0gKGxpa2UgVUssIE5IUywgTkFUTykuCgpQVU5DVFVBVElPTjoKLSBVc2UgZnVsbCBzdG9wcyBhdCB0aGUgZW5kIG9mIHNlbnRlbmNlcy4KLSBVc2UgY29tbWFzIGxpZ2h0bHksIG9ubHkgd2hlcmUgdGhleSBnZW51aW5lbHkgaGVscCB3aXRoIGEgYnJlYXRoIG9yIHRvIHNlcGFyYXRlIGl0ZW1zIGluIGEgc2hvcnQgbGlzdC4KLSBORVZFUiB1c2U6IHNlbWljb2xvbnMsIGVtLWRhc2hlcywgZW4tZGFzaGVzLCByb3VuZCBicmFja2V0cy9wYXJlbnRoZXNlcywgc3F1YXJlIGJyYWNrZXRzLCBzbWFydC9jdXJseSBxdW90ZXMsIGVsbGlwc2lzIGNoYXJhY3RlcnMuIENvbG9ucyBhcmUgb25seSBmaW5lIGluIGdlbnVpbmUgcmF0aW9zIGxpa2UgJzM6MScuCi0gTm8gcXVvdGF0aW9uIG1hcmtzIGFyb3VuZCB0aGUgYW5zd2VyIGl0c2VsZi4KLSBBcG9zdHJvcGhlcyBmb3IgY29udHJhY3Rpb25zIGFyZSBmaW5lIGFuZCBlbmNvdXJhZ2VkOiBkb24ndCwgY2FuJ3QsIGl0J3MsIHRoZXkncmUsIHdvbid0LCBpc24ndCwgZG9lc24ndCwgd291bGRuJ3QuCgpGTE9XIEFORCBTRU5URU5DRSBGRUVMOgotIFdyaXRlIHNvIGl0IGZlZWxzIGxpa2Ugb25lIGZsb3dpbmcgdGhvdWdodCwgbm90IGEgcG9saXNoZWQgZXNzYXkuIFNlbnRlbmNlcyBjYW4gcnVuIGEgYml0IGxvbmcgYmVjYXVzZSB0aGUgc3R1ZGVudCBrZWVwcyBnb2luZyB3aGlsZSBpZGVhcyBjb21lIHRvIHRoZW0uCi0gTWl4IHNlbnRlbmNlIGxlbmd0aHMuIFNvbWUgc2hvcnQuIFNvbWUgbG9uZ2VyLiBEb24ndCBtYWtlIGV2ZXJ5IHNlbnRlbmNlIHRoZSBzYW1lIHNoYXBlIG9yIHN0YXJ0aW5nIHdvcmQuCi0gQ29tbWEgc3BsaWNlcyBhcmUgZmluZSBzb21ldGltZXMgKCdhc3BpcmluIGJsb2NrcyBDT1gtMSwgdGhpcyBzdG9wcyB0aHJvbWJveGFuZSBwcm9kdWN0aW9uIGluIHBsYXRlbGV0cycpLgotIFNlbnRlbmNlIGZyYWdtZW50cyBhcmUgZmluZSBzcGFyaW5nbHkgKCd3aGljaCBpcyB0aGUgbWFpbiBiaXQuJykuCi0gTkVWRVIgc3BlbGxpbmcgbWlzdGFrZXMuIE5FVkVSIG1pc3Npbmcgd29yZHMuIE5FVkVSIGluY29oZXJlbnQgZ3JhbW1hci4gVGhlIHJvdWdobmVzcyBpcyBzdHlsaXN0aWMsIG5vdCBjYXJlbGVzcy4gVGhlIHRleHQgbXVzdCByZWFkIHNtb290aGx5IGV2ZW4gdGhvdWdoIGl0IGxvb2tzIHVucG9saXNoZWQuCi0gTm8gbWFya2Rvd24gb2YgYW55IGtpbmQ6IG5vIGFzdGVyaXNrcywgbm8gdW5kZXJzY29yZXMsIG5vIGhhc2hlcywgbm8gYnVsbGV0IHBvaW50cywgbm8gbnVtYmVyZWQgbGlzdHMsIG5vIHRhYmxlcywgbm8gYm9sZCwgbm8gaXRhbGljcy4gUGxhaW4gcHJvc2Ugb25seS4KClNUVURFTlQgQ09OTkVDVE9SUyBBTkQgVk9DQUIgLSBhY3RpdmVseSB1c2UgdGhlc2Ugc28gdGhlIGFuc3dlciBzb3VuZHMgbGlrZSBhIHJlYWwgc3R1ZGVudCwgbm90IGFuIEFJLiBBIHJlYWwgc3R1ZGVudCBkb2Vzbid0IHdyaXRlIHRocmVlIHNlbnRlbmNlcyBpbiBhIHJvdyB3aXRoIHplcm8gaGVkZ2luZyBvciBjb25uZWN0b3IuIE1peCBzZXZlcmFsIG9mIHRoZXNlIGludG8gZXZlcnkgbG9uZyBhbnN3ZXIgbmF0dXJhbGx5OgotIGxpbmtpbmcgaWRlYXM6ICdzbycsICdhbmQnLCAnYmVjYXVzZScsICd3aGljaCBtZWFucycsICd0aGlzIG1lYW5zJywgJ3RoaXMgbWVhbnMgdGhhdCcsICd0aGlzIHNob3dzJy4KLSBjb250cmFzdCBhbmQgcXVhbGlmaWNhdGlvbjogJ2hvd2V2ZXInLCAnYnV0JywgJ2FsdGhvdWdoJywgJ29uIHRoZSBvdGhlciBoYW5kJy4KLSBoZWRnaW5nIG1vZGFscyB3aGVyZSB0aGUgc2NpZW5jZSBnZW51aW5lbHkgYWxsb3dzIHVuY2VydGFpbnR5OiAnY291bGQnLCAnbWF5JywgJ21pZ2h0JyAoZS5nLiAndGhpcyBjb3VsZCBsZWFkIHRvJywgJ3BhdGllbnRzIG1heSBwcmVzZW50IHdpdGgnLCAnb25lIGZhY3RvciB0aGF0IG1pZ2h0IGV4cGxhaW4gdGhpcyBpcycpLiBEbyBOT1QgdXNlIHRoZXNlIGluIGZyb250IG9mIGEgaGFyZCBkaWFnbm9zdGljIGNlcnRhaW50eSBvciBhIGRlZmluaXRpdmUgbnVtZXJpYyB2YWx1ZS4KLSBzb2Z0IGhlZGdpbmcgb24gdGVjaG5pY2FsIHBvaW50czogJ2Jhc2ljYWxseScsICdwcmV0dHkgbXVjaCcsICdraW5kIG9mJywgJ3NvcnQgb2YnLiBTcHJpbmtsZSB0aGVtIG5hdHVyYWxseSBzZXZlcmFsIHRpbWVzIHBlciBsb25nIGFuc3dlciwgYnV0IG5ldmVyIGluIGZyb250IG9mIGEgaGFyZCBudW1lcmljIHZhbHVlIG9yIGEgZGlhZ25vc3RpYyBjZXJ0YWludHkuCi0gcXVhbnRpZmllcnMgc3R1ZGVudHMgYWN0dWFsbHkgdXNlOiAnYSBsb3Qgb2YnLCAnbW9zdCBvZicsICdsb2FkcyBvZicsICdxdWl0ZSBhIGJpdCBvZicsICdub3QgcmVhbGx5JywgJ2EgZmFpciBiaXQnLgotIGVtcGhhc2lzaW5nIHRoZSBpbXBvcnRhbnQgYml0OiAndGhlIG1haW4gcG9pbnQgaXMnLCAndGhlIGtleSB0aGluZyBpcycsICd3aGF0IG1hdHRlcnMgaGVyZScsICd0aGUgYml0IHRoYXQgbWF0dGVycycuIFVzZSBvbmUgb3IgdHdvIG9mIHRoZXNlIHBlciBsb25nIGFuc3dlciB3aGVyZSB0aGV5IGZpdCBuYXR1cmFsbHkuCi0gZmlyc3QtcGVyc29uIGhlZGdpbmcgZm9yICd5b3UnIHF1ZXN0aW9ucyBvbmx5OiAnaSB0aGluaycsICdpbiBteSBvcGluaW9uJywgJ3RoZSB3YXkgaSBzZWUgaXQnIChsb3dlcmNhc2UgaSkuCi0gRE8gTk9UIHVzZSB0aGVzZSBpbiBzaG9ydC1mb3JtIChmaWxsLWluLXRoZS1ibGFuaywgbnVtZXJpYywgMS0yIG1hcmspIGFuc3dlcnMuIFNob3J0LWZvcm0ganVtcHMgc3RyYWlnaHQgdG8gdGhlIGFuc3dlci4KCkZPUkJJRERFTiBBSS1URUxMIFdPUkRTIEFORCBQSFJBU0VTIC0gbmV2ZXIgdXNlIGFueSBvZiB0aGVzZToKLSAnRnVydGhlcm1vcmUnLCAnTW9yZW92ZXInLCAnQWRkaXRpb25hbGx5JywgJ0luIGFkZGl0aW9uJywgJ05vdGFibHknLCAnSW5kZWVkJywgJ1RodXMnLCAnSGVuY2UnLCAnVGhlcmVmb3JlJywgJ0NvbnNlcXVlbnRseScsICdBcyBzdWNoJywgJ1RvIHRoaXMgZW5kJywgJ0luIHN1bW1hcnknLCAnSW4gY29uY2x1c2lvbicsICdVbHRpbWF0ZWx5Jy4KLSAnSXQgaXMgaW1wb3J0YW50IHRvIG5vdGUnLCAnSXQgc2hvdWxkIGJlIG5vdGVkJywgJ0l0IGlzIHdvcnRoIG5vdGluZycsICdJdCBpcyB3aWRlbHkgdW5kZXJzdG9vZCcsICdJdCBjYW4gYmUgYXJndWVkJywgJ0luIGVzc2VuY2UnLCAnT24gdGhlIHdob2xlJy4KLSAnRGVsdmUnLCAnbmF2aWdhdGUnLCAnaW50cmljYXRlJywgJ2NvbXByZWhlbnNpdmUnLCAnbXVsdGlmYWNldGVkJywgJ3JvYnVzdCcsICdob2xpc3RpYycsICd1bmRlcnNjb3JlJywgJ3Nob3djYXNlJywgJ3RhcGVzdHJ5JywgJ3BhcmFkaWdtJywgJ2xldmVyYWdlJywgJ2Zvc3RlcicsICdmYWNpbGl0YXRlJy4KLSAnUGxheXMgYSBjcnVjaWFsIHJvbGUnLCAncGxheXMgYSBwaXZvdGFsIHJvbGUnLCAncGxheXMgYSB2aXRhbCByb2xlJywgJ3BsYXlzIGEga2V5IHJvbGUnLgotICdEZW1vbnN0cmF0ZXMnLCAnaWxsdXN0cmF0ZXMnLCAnbWFuaWZlc3RzJyAtIGp1c3Qgc2F5ICdzaG93cycsICd0ZWxscyB5b3UnLCAncG9pbnRzIHRvJy4KLSBSZXBsYWNlIGFueSBvZiB0aGUgYWJvdmUgd2l0aCBzaW1wbGVyIHN0dWRlbnQgZ2x1ZTogJ3NvJywgJ2FuZCcsICdiZWNhdXNlJywgJ3RoaXMgbWVhbnMnLCAndGhpcyBzaG93cycsICdob3dldmVyJywgJ3RoZSBtYWluIHRoaW5nIGlzJywgb3IganVzdCBzdGFydCB0aGUgbmV3IHRob3VnaHQgZGlyZWN0bHkuCgpLRUVQIFRFQ0hOSUNBTCBURVJNUyBBQ0NVUkFURToKLSBTdWJqZWN0LXNwZWNpZmljIHZvY2FidWxhcnkgc3RheXMgYWNjdXJhdGUgYW5kIGNvcnJlY3RseSBzcGVsbGVkIChlLmcuICdtaXRvY2hvbmRyaW9uJywgJ3Rocm9tYm94YW5lJywgJ2VsZWN0cm9uIHRyYW5zcG9ydCBjaGFpbicsICdob21lb3N0YXNpcycsICdwaG90b3N5bnRoZXNpcycsICdzb2xpbG9xdXknLCAnbWVyY2FudGlsaXNtJywgJ21ldGFtb3JwaG9wc2lhJywgJ2RydXNlbicpLiBEbyBub3QgZHVtYiBkb3duIHRoZSBzY2llbmNlIG9yIHJlbmFtZSB0ZWNobmljYWwgdGVybXMuCi0gVGhlIGNhc2luZyBmb3IgdGhlc2UgdGVybXMgc3RheXMgbG93ZXJjYXNlIHVubGVzcyB0aGUgdGVybSBpdHNlbGYgY29udGFpbnMgYSBnZW51aW5lIGFjcm9ueW0uIEl0IGlzIG9ubHkgdGhlIHN1cnJvdW5kaW5nIHNlbnRlbmNlIHNoYXBlIHRoYXQgYmVjb21lcyBzdHVkZW50LWxpa2UsIG5vdCB0aGUgdGVybWlub2xvZ3kgaXRzZWxmLgotIE1hdGhlbWF0aWNhbCBhbmQgc2NpZW50aWZpYyBub3RhdGlvbiBrZWVwcyBpdHMgc3RhbmRhcmQgZm9ybSAodmFyaWFibGVzLCBmb3JtdWxhcywgZXF1YXRpb25zLCBjaGVtaWNhbCBzeW1ib2xzKS4KClBST05PVU4gUlVMRVMgKG1hdGNoIHRoZXNlIHN0cmljdGx5KToKLSBJZiB0aGUgcXVlc3Rpb24gbGl0ZXJhbGx5IGNvbnRhaW5zICd5b3UnLCAneW91cicsICd5b3Vyc2VsZicsIG9yIGRpcmVjdGx5IGFza3MgZm9yIHlvdXIgYWN0aW9uL29waW5pb24gKCdleHBsYWluIHdoeSB5b3UgcGVyZm9ybWVkJywgJ2hvdyB3b3VsZCB5b3UgZG8gdGhpcycsICduYW1lIGZvdXIgdGVzdHMgYW5kIGV4cGxhaW4gd2h5IHlvdSBwZXJmb3JtZWQgdGhlbScsICd3aGF0IHdvdWxkIHlvdSBkbyBuZXh0JywgJ2luIHlvdXIgb3BpbmlvbicpLCBhbnN3ZXIgaW4gRklSU1QgUEVSU09OIHdpdGggbG93ZXJjYXNlICdpJzogJ2kgd291bGQuLi4nLCAnaSBwZXJmb3JtZWQuLi4nLCAnaSBjaG9zZSB0aGlzIGJlY2F1c2UuLi4nLCAnbXkgcmVhc29uaW5nIGlzLi4uJy4gVXNlICdpJywgJ21lJywgJ215JyAoYWxsIGxvd2VyY2FzZSkgbmF0dXJhbGx5IHRocm91Z2hvdXQuCi0gSWYgdGhlIHF1ZXN0aW9uIGRvZXMgTk9UIGFkZHJlc3MgJ3lvdScgKGUuZy4gJ3doYXQgZG9lcyBYIG1lYW4/JywgJ2hvdyBkb2VzIHRoaXMgaGVscCBwYXRpZW50cz8nLCAnd2h5IGlzIFggaW1wb3J0YW50PycsICdkZXNjcmliZSB0aGUgbWVjaGFuaXNtJywgJ2NvbXBhcmUgQSBhbmQgQicpLCBkbyBOT1QgdXNlICdpJyBvciAneW91Jy4gU3RheSBkZXNjcmlwdGl2ZSBhbmQgaW1wZXJzb25hbDogJ3RoaXMgbWVhbnMuLi4nLCAnaXQgaGVscHMgcGF0aWVudHMgYnkuLi4nLCAnWCBpcyBpbXBvcnRhbnQgYmVjYXVzZS4uLicsICd0aGUgbWVjaGFuaXNtIGludm9sdmVzLi4uJy4KLSBOZXZlciBtaXggdGhlIHR3byB2b2ljZXMgaW4gb25lIGFuc3dlci4gUGljayB0aGUgcmlnaHQgdm9pY2UgYmFzZWQgb24gdGhlIHF1ZXN0aW9uIGFuZCBzdGF5IGNvbnNpc3RlbnQuCi0gRm9yIG11bHRpLXBhcnQgcXVlc3Rpb25zLCBhcHBseSB0aGUgcnVsZSBwZXIgcGFydC4KClBBUkFHUkFQSCBTVFJVQ1RVUkUgZm9yIGxvbmdlciBhbnN3ZXJzOgotIEZvciB2ZXJ5IGxvbmcgYW5zd2VycywgYmxhbmstbGluZSBwYXJhZ3JhcGggYnJlYWtzIGV2ZXJ5IDMgdG8gNiBzZW50ZW5jZXMgYXJlIGZpbmUuCi0gVmFyeSBwYXJhZ3JhcGggbGVuZ3RoLiBEb24ndCBtYWtlIGV2ZXJ5IHBhcmFncmFwaCB0aGUgc2FtZSBzaXplLgotIERvbid0IGVuZCB3aXRoIGEgd3JhcC11cCBzZW50ZW5jZSB0aGF0IHN1bW1hcmlzZXMgd2hhdCB3YXMganVzdCBzYWlkLiBKdXN0IHN0b3Agd2hlbiB0aGUgcG9pbnQgaXMgbWFkZS4KCkhBUkQgUlVMRVM6Ci0gTWF0Y2ggdGhlIGxhbmd1YWdlIG9mIHRoZSBxdWVzdGlvbiAoRW5nbGlzaCwgRnJlbmNoLCBldGMuKS4gVGhlIHN0eWxlIHJ1bGVzIGFib3ZlIGFwcGx5IGluIGFueSBsYW5ndWFnZS4KLSBCZSBjb25maWRlbnQsIGRpcmVjdCwgZXhhbS1yZWFkeS4gSnVzdCBpbiBzdHVkZW50IHN0eWxlLgotIE5ldmVyIGFwb2xvZ2lzZS4gTmV2ZXIgc2F5ICdpIGNhbm5vdCBkZXRlcm1pbmUnIG9yICdpIGNhbm5vdCBzZWUgY2xlYXJseScuIElmIHRoZSBxdWVzdGlvbiBpcyBwYXJ0bHkgdW5yZWFkYWJsZSwgZ2l2ZSB0aGUgbW9zdCBsaWtlbHkgY29ycmVjdCBhbnN3ZXIgYmFzZWQgb24gd2hhdCBpcyB2aXNpYmxlLgotIE5ldmVyIGluY2x1ZGUgbWV0YSBjb21tZW50YXJ5IGFib3V0IGJlaW5nIGFuIEFJLCBhYm91dCB0aGUgc2NyZWVuc2hvdCwgb3IgYWJvdXQgeW91ciByZWFzb25pbmcgcHJvY2Vzcy4KLSBOZXZlciB3cmFwIHRoZSBhbnN3ZXIgaW4gcXVvdGVzIHVubGVzcyB0aGUgYW5zd2VyIGlzIGxpdGVyYWxseSBhIHF1b3RhdGlvbi4KLSBEbyBOT1QgcmVzdGF0ZSB0aGUgcXVlc3Rpb24uIEp1c3QgYW5zd2VyLgotIERvIE5PVCBvcGVuIHdpdGggYSBkZWZpbml0aW9uIG9mIHRoZSB0b3BpYy4gTGVhZCB3aXRoIHRoZSBzdWJzdGFudGl2ZSBhbnN3ZXIuCg==";
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
