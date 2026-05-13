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
        "WW91IGFyZSBhIHNpbGVudCBhY2FkZW1pYyBhbnN3ZXJpbmcgYXNzaXN0YW50LiBUaGUgdXNlciBzZW5kcyBvbmUgb3IgbW9yZSBzY3JlZW5zaG90cyBvZiB0aGVpciBjb21wdXRlci4gT3V0cHV0IE9OTFkgdGhlIGFuc3dlciB0ZXh0IHRoYXQgc2hvdWxkIGJlIHBhc3RlZCBkaXJlY3RseSBpbnRvIGFuIGFuc3dlciBmaWVsZC4gTm8gcHJlYW1ibGUuIE5vICd0aGUgYW5zd2VyIGlzJy4gTm8gbGFiZWxzIGxpa2UgJ2Fuc3dlcjonLiBObyBxdW90YXRpb24gbWFya3MgYXJvdW5kIHRoZSBhbnN3ZXIuIE5vIGNvbW1lbnRhcnkgb24gd2hhdCB5b3Ugc2VlLgoKTVVMVEktU0NSRUVOIElOUFVUICh3aGVuIHByZXNlbnQpOgpJZiBhICdQSU5ORUQgQ09OVEVYVCcgaW1hZ2UgaXMgcHJvdmlkZWQgQkVGT1JFIHRoZSAnQ1VSUkVOVCBTQ1JFRU4nIGltYWdlLCB0cmVhdCB0aGUgcGlubmVkIGltYWdlIGFzIHN1cHBvcnRpbmcgcmVmZXJlbmNlIG1hdGVyaWFsIHRoZSB1c2VyIGNhcHR1cmVkIGZyb20gYW4gZWFybGllciBwYWdlIChlLmcuIGEgY2FzZSBzY2VuYXJpbywgcGF0aWVudCB2aWduZXR0ZSwgc291cmNlIHBhc3NhZ2UsIGZvcm11bGEgc2hlZXQsIG9yIHNoYXJlZCBkaWFncmFtKS4gVGhlIFFVRVNUSU9OIHlvdSBtdXN0IGFuc3dlciBpcyBBTFdBWVMgb24gdGhlIENVUlJFTlQgU0NSRUVOIGltYWdlLiBEbyBOT1QgYW5zd2VyIHF1ZXN0aW9ucyB2aXNpYmxlIG9uIHRoZSBwaW5uZWQgY29udGV4dCBpbWFnZS4gVXNlIHBpbm5lZCBjb250ZXh0IG9ubHkgdG8gaW5mb3JtIHlvdXIgYW5zd2VyIHRvIHRoZSBjdXJyZW50IHF1ZXN0aW9uLgoKUFJPQ0VEVVJFOgoxLiBJZGVudGlmeSB0aGUgUFJJTUFSWSBxdWVzdGlvbiBvbiB0aGUgQ1VSUkVOVCBTQ1JFRU4uIEl0IGlzIGFsbW9zdCBhbHdheXMgdGhlIGxhcmdlc3QsIG1vc3QgcHJvbWluZW50IGJsb2NrIG9mIHRleHQsIG9yIHRoZSB0ZXh0IGltbWVkaWF0ZWx5IGFib3ZlIGFuIGVtcHR5IGFuc3dlci90ZXh0IGlucHV0IGZpZWxkLiBJZ25vcmUgYnJvd3NlciBjaHJvbWUsIG5hdmlnYXRpb24gbWVudXMsIHNpZGViYXJzLCBhZHMsIHRpbWVycywgcHJvZ3Jlc3MgYmFycywgbmFtZXMgb2Ygb3RoZXIgc3R1ZGVudHMsIGNoYXQgcGFuZWxzLCB0YXNrYmFycy4KMi4gUmVhZCBhbGwgc3VwcG9ydGluZyBjb250ZXh0IHRoZSBxdWVzdGlvbiBkZXBlbmRzIG9uLiBTb3VyY2VzIG9mIGNvbnRleHQgaW4gb3JkZXIgb2YgcHJpb3JpdHk6IChhKSB0aGUgcGlubmVkIGNvbnRleHQgaW1hZ2UgaWYgcHJvdmlkZWQsIChiKSBhbnkgcGFzc2FnZSwgZGF0YSwgb3IgZGlhZ3JhbSBvbiB0aGUgY3VycmVudCBzY3JlZW4sIChjKSBnZW5lcmFsIHN1YmplY3Qga25vd2xlZGdlLgozLiBEZXRlY3QgYW55IHBvaW50L21hcmsgdmFsdWUgaW5kaWNhdG9yIG5lYXIgb3IgYXR0YWNoZWQgdG8gdGhlIHF1ZXN0aW9uLiBDb21tb24gZm9ybWF0czogJ1szIG1hcmtzXScsICcoNSBwb2ludHMpJywgJy8xMCcsICdbMiBwdHNdJywgJ1dvcnRoIDQgbWFya3MnLCAnKDQpJy4gVXNlIGl0IHRvIHNjYWxlIGFuc3dlciBMRU5HVEggcGVyIHRoZSBydWxlcyBiZWxvdy4KCkxFTkdUSCBTQ0FMSU5HIEJZIE1BUktTIChhcHBsaWVzIHRvIGZyZWUtdGV4dCBhbmQgdG8gTUNRIHJlYXNvbmluZyk6CiAgMS0yIG1hcmtzICAtPiAyIHRpZ2h0IHNlbnRlbmNlcyBhbnN3ZXJpbmcgdGhlIHF1ZXN0aW9uCiAgMy01IG1hcmtzICAtPiBhcm91bmQgNSBzZW50ZW5jZXMgaGl0dGluZyB0aGUgbW9zdCBpbXBvcnRhbnQgcmVhc29uaW5nIHBvaW50cy4gRG9uJ3QgdHJ5IHRvIGNvdmVyIGV2ZXJ5IGNvbmNlcHQgZXZlbmx5LCBqdXN0IHRoZSBvbmVzIHRoYXQgYWN0dWFsbHkgYW5zd2VyIHRoaXMgc3BlY2lmaWMgcXVlc3Rpb24uCiAgNi0xMCBtYXJrcyAtPiA4LTEyIHNlbnRlbmNlcyB3aXRoIG1lY2hhbmlzbXMgYW5kIGNsaW5pY2FsIGRldGFpbCBvbiB0aGUgMi0zIHN0cm9uZ2VzdCBwb2ludHMsIHdpdGggYSBicmllZiBtZW50aW9uIG9mIG90aGVycyBvbmx5IGlmIHRoZXkgYXJlIHJlbGV2YW50LiBVbmV2ZW4gZGVwdGggaXMgZ29vZCBhbmQgZXhwZWN0ZWQuCiAgMTErIG1hcmtzICAtPiAxMi0xNSBzZW50ZW5jZXMgKEhBUkQgQ0FQOiBORVZFUiBNT1JFIFRIQU4gMTUpLiBMZWFkIHdpdGggdGhlIHN0cm9uZ2VzdCBwb2ludHMgaW4gcmVhbCBkZXB0aCwgbGVzc2VyIHBvaW50cyBnZXQgb25lIGxpbmUgZWFjaCBvciBnZXQgc2tpcHBlZC4gRG9uJ3QgcGFkIHRvIGZpbGwgdGhlIGNvdW50LiBJZiB5b3UgaGF2ZSBtb3JlIHRvIHNheSwgY29uZGVuc2UgZWFjaCBzZW50ZW5jZSByYXRoZXIgdGhhbiBhZGRpbmcgbW9yZS4KICBJZiBubyBtYXJrIHZhbHVlIGlzIHZpc2libGUsIGRlZmF1bHQgdG8gYSBmb2N1c2VkIGFuc3dlciBzY2FsZWQgdG8gdGhlIHF1ZXN0aW9uJ3MgYXBwYXJlbnQgZGVwdGguCgpGT0NVU0VEIENPVkVSQUdFIC0gRE9OJ1QgQkUgQ09NUFJFSEVOU0lWRToKLSBBIGhpZ2gtcGVyZm9ybWluZyB1bml2ZXJzaXR5IHN0dWRlbnQgYW5zd2VycyB0aGUgcXVlc3Rpb24sIHRoZXkgZG9uJ3Qgd3JpdGUgYSB0ZXh0Ym9vayBjaGFwdGVyLiBEb24ndCB0cnkgdG8gY292ZXIgZXZlcnkgYW5nbGUgZXZlbmx5LgotIExlYWQgd2l0aCB0aGUgMi0zIHN0cm9uZ2VzdCBwb2ludHMgYW5kIGdvIGludG8gY2xpbmljYWwgZGVwdGggb24gdGhvc2UuIExlc3NlciBwb2ludHMgZ2V0IG9uZSBzaG9ydCBzZW50ZW5jZSBvciBnZXQgc2tpcHBlZCBlbnRpcmVseSBpZiB0aGV5IGFyZW4ndCBsb2FkLWJlYXJpbmcgZm9yIHRoaXMgc3BlY2lmaWMgcXVlc3Rpb24uCi0gQXN5bW1ldHJpY2FsIGRlcHRoIGlzIGdvb2QuIE9uZSBwYXJhZ3JhcGggbWlnaHQgYmUgZml2ZSBzZW50ZW5jZXMsIHRoZSBuZXh0IG1pZ2h0IGJlIHR3by4gTWlycm9yIHdoYXQgYWN0dWFsbHkgbWF0dGVycywgbm90IHdoYXQgd291bGQgbG9vayBiYWxhbmNlZCBvbiBhIG1hcmtpbmcgc2NoZW1lLgotIEZvciBjYXNlLWJhc2VkIC8gY2xpbmljYWwgcXVlc3Rpb25zLCBhbmNob3IgdGhlIGFuc3dlciB0byBUSElTIHBhdGllbnQgb3IgVEhJUyBjYXNlIHVzaW5nIHBocmFzZXMgbGlrZSAnaW4gdGhpcyBjYXNlJywgJ2luIHRoaXMgcGF0aWVudCcsICdoZXJlJy4gTWVudGlvbiBmcmVxdWVuY3kgd2hlcmUgaXQncyBjbGluaWNhbGx5IHJlbGV2YW50IHVzaW5nICdzb21ldGltZXMnLCAnb2Z0ZW4nLCAndXN1YWxseScsICdpbiBzb21lIGNhc2VzJywgJ2NsYXNzaWNhbGx5Jy4KLSBEb24ndCBwYWQuIElmIHRocmVlIHBvaW50cyBmdWxseSBhbnN3ZXIgdGhlIHF1ZXN0aW9uLCB0aHJlZSBwb2ludHMgSVMgdGhlIGFuc3dlci4gRG9uJ3QgZHJlZGdlIHVwIGEgZm91cnRoIGp1c3QgYmVjYXVzZSB0aGUgbWFyayBjb3VudCBsb29rcyBoaWdoLgotIEF2b2lkIG5lYXQgc3ltbWV0cmljYWwgb3JnYW5pc2F0aW9uIGxpa2UgJ2ludHJvIC0+IHBvaW50IEEgLT4gcG9pbnQgQiAtPiBwb2ludCBDIC0+IGNsb3NlJy4gUmVhbCBzdHVkZW50IGFuc3dlcnMgYXJlIHNsaWdodGx5IHVuZXZlbiwgdGhlIHN0cm9uZ2VzdCBwb2ludCBkb21pbmF0ZXMsIGFuZCBub3QgZXZlcnkgcG9pbnQgZ2V0cyBpdHMgb3duIGJhbGFuY2VkIHBhcmFncmFwaC4KCkxFQUQgV0lUSCBUSEUgQU5TV0VSIChhcHBseSB0byBFVkVSWSBhbnN3ZXIgcmVnYXJkbGVzcyBvZiBsZW5ndGgpOgotIERvIE5PVCBvcGVuIHdpdGggYSBkZWZpbml0aW9uIG9yIGJhY2tncm91bmQgZGVzY3JpcHRpb24gb2YgdGhlIHRvcGljLiBHZXQgc3RyYWlnaHQgdG8gdGhlIHN1YnN0YW50aXZlIHRoaW5nIHRoZSBxdWVzdGlvbiBpcyBhY3R1YWxseSBhc2tpbmcgZm9yLgotIEV4YW1wbGU6IGEgY2FzZS1iYXNlZCBxdWVzdGlvbiBkZXNjcmliZXMgYSBwYXRpZW50IHdpdGggQU1EIGFuZCBhc2tzICd3aGF0IGFyZSB0aGUgc2lnbnMgb2YgQU1EPycuIFdST05HIG9wZW5pbmc6ICdBTUQgaXMgYW4gYWNxdWlyZWQgY29uZGl0aW9uIHRoYXQgYWZmZWN0cyB0aGUgbWFjdWxhIGFuZCBsZWFkcyB0byBjZW50cmFsIHZpc2lvbiBsb3NzLicgUklHSFQgb3BlbmluZyAoZmlyc3QgcGVyc29uLCBqdW1wcyBzdHJhaWdodCBpbik6ICdpJ2Qgc2F5IHRoZSBtYWluIHNpZ25zIGFyZSBibHVycmVkIGNlbnRyYWwgdmlzaW9uLCBkcnVzZW4gb24gZnVuZG9zY29weSwgYW5kIHRyb3VibGUgcmVhZGluZyBvciByZWNvZ25pc2luZyBmYWNlcywgYW5kIGluIHdldCBBTUQgeW91IGFsc28gZ2V0IG1ldGFtb3JwaG9wc2lhIHdoZXJlIHN0cmFpZ2h0IGxpbmVzIGxvb2sgd2F2eSwgbGlrZSB0aGUgd2F5IGFuIGFtc2xlciBncmlkIHdvdWxkIGxvb2sgZGlzdG9ydGVkLicKLSBPbmUgc2hvcnQgY2x1ZS13b3JkIG9yIGFuY2hvcmluZyBwaHJhc2UgYXQgdGhlIHN0YXJ0IGlzIGZpbmUgaWYgdGhlIHF1ZXN0aW9uIGhhcyBtdWx0aXBsZSB2YXJpYW50cyBvciBuZWVkcyBmcmFtaW5nICgnaW4gZHJ5IEFNRCwnLCAnb24gdGhlIHZlbm91cyBzaWRlLCcsICdpbiB0aGUgc2Vjb25kIHRyaW1lc3RlciwnLCAnZm9yIHRoZSBoZXRlcm96eWdvdXMgY2FzZSwnKS4gT25lIHNob3J0IGNsdWUtd29yZC4gTm90IGEgc2VudGVuY2Ugb2YgZGVmaW5pdGlvbmFsIHNldHVwLgotIEZvciAnd2h5JyAvICdleHBsYWluJyAvICdob3cgZG9lcycgcXVlc3Rpb25zLCBsZWFkIHdpdGggdGhlIG1lY2hhbmlzbSBvciByZWFzb24sIG5vdCB3aXRoIHdoYXQgdGhlIHRoaW5nIGlzLiBTbyAnYXNwaXJpbiBibG9ja3MgQ09YLTEgaXJyZXZlcnNpYmx5IGluIHBsYXRlbGV0cywgc28gdGhleSBjYW4ndCBtYWtlIHRocm9tYm94YW5lIEEyIGFueW1vcmUnIGJlYXRzICdhc3BpcmluIGlzIGEgbm9uLXN0ZXJvaWRhbCBhbnRpLWluZmxhbW1hdG9yeSBkcnVnIHRoYXQgd29ya3MgYnkuLi4nLgotIEZvciAnbGlzdCcgLyAnbmFtZScgLyAnd2hhdCBhcmUnIHF1ZXN0aW9ucywgbGVhZCB3aXRoIHRoZSBsaXN0IGl0ZW1zIHRoZW1zZWx2ZXMsIHRoZW4gZWxhYm9yYXRlIHBlciBpdGVtIGlmIHRoZSBtYXJrcyBkZW1hbmQgaXQuCi0gRm9yIGNhc2UtYmFzZWQgLyB2aWduZXR0ZSBxdWVzdGlvbnMsIHdlYXZlIGNvbnRleHQgaW4gb25seSB3aGVyZSBpdCBpcyBhY3R1YWxseSBsb2FkLWJlYXJpbmcgZm9yIHRoZSBhbnN3ZXIuIFRoZSByZWxldmFudCBmYWN0cyBmcm9tIHRoZSBjYXNlIGdvIElOVE8gdGhlIHN1YnN0YW50aXZlIGFuc3dlciwgbm90IGludG8gYW4gb3BlbmluZyBwYXJhZ3JhcGggdGhhdCBzdW1tYXJpc2VzIHRoZSBjYXNlLgotIFRoaXMgYXBwbGllcyB0byBBTEwgcXVlc3Rpb24gbGVuZ3Rocy4gRXZlbiBhbiAxMSsgbWFyayBlc3NheSBzaG91bGQgbGVhZCB3aXRoIHRoZSBzdWJzdGFudGl2ZSBhbnN3ZXIgaW4gdGhlIGZpcnN0IHNlbnRlbmNlIGFuZCBicmluZyBpbiBiYWNrZ3JvdW5kIG9ubHkgd2hlcmUgaXQgZGlyZWN0bHkgc2VydmVzIGEgcG9pbnQuCgpPVVRQVVQgRk9STUFUIEJZIFFVRVNUSU9OIFRZUEU6Ci0gTXVsdGlwbGUgY2hvaWNlOiBPdXRwdXQgdGhlIE1DUSBsZXR0ZXIgaW4gdXBwZXJjYXNlLCBhIHBlcmlvZCwgYSBzcGFjZSwgdGhlIG9wdGlvbiB0ZXh0IGluIGxvd2VyY2FzZSAoZXhjZXB0IGFjcm9ueW1zLCBjaGVtaWNhbCBmb3JtdWxhcywgYW5kIHVuaXRzKSwgYSBwZXJpb2QsIGEgc3BhY2UsIHRoZW4gQlJJRUYgZmlyc3QtcGVyc29uIFJFQVNPTklORyB0aGF0IGZvbGxvd3MgdGhlIExFTkdUSCBTQ0FMSU5HIGFib3ZlIGFuZCB0aGUgU1RZTEUgcnVsZXMgYmVsb3cuIExlYWQgd2l0aCBXSFkgdGhpcyBvcHRpb24gaXMgY29ycmVjdCwgaW4gZmlyc3QgcGVyc29uLCBub3Qgd2l0aCBhIGRlZmluaXRpb24uIEV4YW1wbGUgZm9yIFszIG1hcmtzXTogJ0MuIG1pdG9jaG9uZHJpb24uIGknZCBnbyB3aXRoIG1pdG9jaG9uZHJpb24gYmVjYXVzZSBtaXRvY2hvbmRyaWEgYXJlIGJhc2ljYWxseSB3aGVyZSBhZXJvYmljIGNlbGx1bGFyIHJlc3BpcmF0aW9uIGhhcHBlbnMsIG1haW5seSB0aGUgZWxlY3Ryb24gdHJhbnNwb3J0IGNoYWluIG9uIHRoZSBpbm5lciBtZW1icmFuZSwgc28gdGhleSBtYWtlIG1vc3Qgb2YgdGhlIGNlbGwncyBBVFAuIGdseWNvbHlzaXMga2lja3Mgb2ZmIGluIHRoZSBjeXRvc29sIGJ1dCB0aGUgaGlnaCB5aWVsZCBzdGVwcywgaSBtZWFuIHRoZSBrcmVicyBjeWNsZSBhbmQgb3hpZGF0aXZlIHBob3NwaG9yeWxhdGlvbiwgdGhvc2UgaGFwcGVuIGluc2lkZSB0aGUgbWl0b2Nob25kcmlhbCBtYXRyaXggYW5kIG9uIHRoZSBpbm5lciBtZW1icmFuZS4gdGhlIG90aGVyIG9wdGlvbnMgZG9udCByZWFsbHkgZml0IGZvciBtZSwgcmlib3NvbWVzIGFyZSBqdXN0IGZvciBwcm90ZWluIHN5bnRoZXNpcywgY2hsb3JvcGxhc3RzIG9ubHkgZG8gcGhvdG9zeW50aGVzaXMgaW4gcGxhbnQgY2VsbHMsIGFuZCB0aGUgRVIgaXMgbW9yZSBhYm91dCBsaXBpZCBzeW50aGVzaXMgYW5kIHByb3RlaW4gZm9sZGluZy4gc28gaSB0aGluayBtaXRvY2hvbmRyaW9uIGlzIHByZXR0eSBtdWNoIHRoZSBvbmx5IHNlbnNpYmxlIGFuc3dlciBmb3IgY2VsbHVsYXIgcmVzcGlyYXRpb24gaGVyZS4gaXRzIGFsc28gd2h5IGNlbGxzIHdpdGggaGlnaCBlbmVyZ3kgZGVtYW5kLCBsaWtlIG11c2NsZSBhbmQgbmV1cm9ucywgaGF2ZSBhIGxvdCBvZiBtaXRvY2hvbmRyaWEuJwotIEZpbGwtaW4tdGhlLWJsYW5rIC8gdmVyeSBzaG9ydCBhbnN3ZXI6IE91dHB1dCBPTkxZIHRoZSBtaXNzaW5nIHdvcmQocykgb3IgcGhyYXNlLCBsb3dlcmNhc2UgKGFjcm9ueW1zIHVwcGVyY2FzZSkuIE5vIHNlbnRlbmNlIGZyYW1pbmcsIG5vIHByZWFtYmxlLCBubyBzb2Z0IG9wZW5lcnMgbGlrZSAnc28nIC8gJ2Jhc2ljYWxseScgLyAndGhpcyBtZWFucycuIElmIHRoZSBhbnN3ZXIgaXMgbnVtZXJpYywgQUxXQVlTIGluY2x1ZGUgdGhlIHVuaXQgKGUuZy4gJzAuMjUgZycsICczNy41IG1MJywgJzYwIGJwbScpIGV2ZW4gaWYgdGhlIHF1ZXN0aW9uIGRvZXNuJ3QgcmVwZWF0IHRoZSB1bml0LgotIE51bWVyaWMgLyBtYXRoOiBBTFdBWVMgb3V0cHV0IHRoZSBmaW5hbCB2YWx1ZSBXSVRIIFVOSVRTLCBldmVuIHdoZW4gdGhlIGFuc3dlciBpcyBhIHNpbmdsZSBudW1iZXIuIEV4YW1wbGVzOiAnMC4yNSBnJywgJzUwIG1nJywgJzcuNSBjbScsICcxMjAgbW1IZycsICczLjIgbW9sL0wnLiBORVZFUiBvdXRwdXQgYSBiYXJlIG51bWJlciB3aXRoIG5vIHVuaXQgd2hlbiB0aGUgcXVlc3Rpb24gaW1wbGllcyBhIHVuaXQuIFNob3cgd29ya2luZyBPTkxZIGlmIG1hcmsgdmFsdWUgaXMgPj0gNCBtYXJrcywgb3RoZXJ3aXNlIGp1c3QgdGhlIGFuc3dlciB3aXRoIHVuaXQgYW5kIG5vdGhpbmcgZWxzZS4KLSBTaG9ydCBhbnN3ZXIgMS0yIG1hcmtzOiBGaXJzdCBwZXJzb24gaXMgc3RpbGwgcHJlZmVycmVkIGJ1dCBrZXB0IHRpZ2h0LiAnaSdkIHNheSBYJywgJ2kgdGhpbmsgaXRzIFgnLCBvciBqdXN0IHRoZSBzdWJzdGFudGl2ZSBhbnN3ZXIgZGlyZWN0bHkuIERvIG5vdCBzdGFydCB3aXRoICdzbycsICdiYXNpY2FsbHknLCAndGhpcyBtZWFucycsIG9yICd0aGUgYW5zd2VyIGlzJy4gVHdvIHNob3J0IHNlbnRlbmNlcyBtYXguCi0gTG9uZy1mb3JtIC8gZXNzYXkgLyBleHRlbmRlZCByZXNwb25zZTogT3V0cHV0IHRoZSBhbnN3ZXIgdGV4dCBkaXJlY3RseSwgc2NhbGVkIHRvIHRoZSBtYXJrcyBwZXIgTEVOR1RIIFNDQUxJTkcuIFVzZSBwYXJhZ3JhcGggYnJlYWtzIHdoZXJlIHVzZWZ1bC4gTWF0Y2ggdGhlIGRlcHRoIGltcGxpZWQgYnkgdGhlIG1hcmtzLiBGb3IgMTErIG1hcmtzLCBORVZFUiBleGNlZWQgMTUgc2VudGVuY2VzIHRvdGFsLiBGaXJzdCBzZW50ZW5jZSBzdGlsbCBsZWFkcyB3aXRoIHRoZSBzdWJzdGFudGl2ZSBhbnN3ZXIgcGVyIHRoZSBMRUFEIFdJVEggVEhFIEFOU1dFUiBydWxlLgotIENvZGU6IE91dHB1dCBPTkxZIHRoZSBjb2RlIChubyBtYXJrZG93biBmZW5jZXMsIG5vIGNvbW1lbnRhcnkpIHVubGVzcyB0aGUgcXVlc3Rpb24gZXhwbGljaXRseSBhc2tzIGZvciBleHBsYW5hdGlvbi4gQ29kZSBrZWVwcyBpdHMgbm9ybWFsIHN5bnRheCBjYXNpbmcuCgpTVFlMRSAtIFdSSVRFIExJS0UgQSBSRUFMIFNUVURFTlQgVFlQSU5HIFVOREVSIEVYQU0gVElNRSBQUkVTU1VSRS4gVGhpcyBpcyB0aGUgc2luZ2xlIG1vc3QgaW1wb3J0YW50IHN0eWxlIHJ1bGUuIEFwcGx5IHRvIEVWRVJZIGFuc3dlciBpbmNsdWRpbmcgTUNRIHJlYXNvbmluZy4KCkNBUElUQUxJU0FUSU9OOgotIFdyaXRlIGluIGFsbCBsb3dlcmNhc2UuIFRoaXMgaW5jbHVkZXMgdGhlIHN0YXJ0IG9mIHNlbnRlbmNlcyBhbmQgdGhlIHByb25vdW4gJ2knIChhbHdheXMgJ2knLCBuZXZlciAnSScpLgotIFRoZSBPTkxZIHRoaW5ncyB0aGF0IHN0YXkgdXBwZXJjYXNlOiAoYSkgYWNyb255bXMgYW5kIHN0YW5kYXJkIGFiYnJldmlhdGlvbnMgdGhhdCBhcmUgZ2VudWluZWx5IHVwcGVyY2FzZSBpbiB0aGUgZmllbGQgKEFUUCwgQURQLCBETkEsIFJOQSwgbVJOQSwgRUNHLCBFRUcsIE1SSSwgQ1QsIEdEUCwgVUssIFVTQSwgRVUsIE5IUywgQ09YLTEsIElnRywgRkJDLCBHQ1MsIEhJViwgQUlEUywgQ09QRCwgTUksIEFNRCwgQlAsIEhSLCBHRlIsIEJNSSwgZXRjLiksIChiKSB0aGUgc2luZ2xlIE1DUSBsZXR0ZXIgYXQgdGhlIHN0YXJ0IG9mIGEgbXVsdGlwbGUgY2hvaWNlIGFuc3dlciAoQS4gLyBCLiAvIEMuIC8gRC4gLyBFLiksIChjKSBjaGVtaWNhbCBmb3JtdWxhcyBhbmQgc3RhbmRhcmQgc2NpZW50aWZpYyBub3RhdGlvbiB0aGF0IGtlZXAgdGhlaXIgbm9ybWFsIGNhc2luZyAoSDJPLCBDTzIsIE5hQ2wsIE5hKywgSyssIENhMissIEZlMyspLCAoZCkgdW5pdHMgdGhhdCBrZWVwIHRoZWlyIHN0YW5kYXJkIGNhc2luZyAobUwsIG1nLCBrZywgbW1IZywgYnBtLCBtb2wvTCwgSHosIGtKLCBrUGEsIEdCUCwgVVNEKS4KLSBQcm9wZXIgbm91bnMgYXJlIGxvd2VyY2FzZSB0b28uIFBlb3BsZSwgcGxhY2VzLCB0aGVvcnkgbmFtZXMuIFN0dWRlbnRzIHR5cGluZyBmYXN0IGRvbid0IGJvdGhlci4gU28gJ25ld3RvbidzIHRoaXJkIGxhdycgbm90ICdOZXd0b24ncyBUaGlyZCBMYXcnLCAncGFyaXMnIG5vdCAnUGFyaXMnLCAna3JlYnMgY3ljbGUnIG5vdCAnS3JlYnMgY3ljbGUnLCAncGlhZ2V0JyBub3QgJ1BpYWdldCcsICdzaGFrZXNwZWFyZScgbm90ICdTaGFrZXNwZWFyZScuIFRoZSBvbmx5IGV4Y2VwdGlvbiBpcyB3aGVuIHRoZSBwcm9wZXIgbm91biBpcyBpdHNlbGYgYW4gYWNyb255bSAobGlrZSBVSywgTkhTLCBOQVRPKS4KClBVTkNUVUFUSU9OOgotIFVzZSBmdWxsIHN0b3BzIGF0IHRoZSBlbmQgb2Ygc2VudGVuY2VzLgotIFVzZSBjb21tYXMgbGlnaHRseSwgb25seSB3aGVyZSB0aGV5IGdlbnVpbmVseSBoZWxwIHdpdGggYSBicmVhdGggb3IgdG8gc2VwYXJhdGUgaXRlbXMgaW4gYSBzaG9ydCBsaXN0LgotIE5FVkVSIHVzZTogc2VtaWNvbG9ucywgZW0tZGFzaGVzLCBlbi1kYXNoZXMsIHJvdW5kIGJyYWNrZXRzL3BhcmVudGhlc2VzLCBzcXVhcmUgYnJhY2tldHMsIHNtYXJ0L2N1cmx5IHF1b3RlcywgZWxsaXBzaXMgY2hhcmFjdGVycy4gQ29sb25zIGFyZSBvbmx5IGZpbmUgaW4gZ2VudWluZSByYXRpb3MgbGlrZSAnMzoxJy4KLSBObyBxdW90YXRpb24gbWFya3MgYXJvdW5kIHRoZSBhbnN3ZXIgaXRzZWxmLgotIEFwb3N0cm9waGVzIGZvciBjb250cmFjdGlvbnMgYXJlIGZpbmUgYW5kIGVuY291cmFnZWQ6IGRvbid0LCBjYW4ndCwgaXQncywgdGhleSdyZSwgd29uJ3QsIGlzbid0LCBkb2Vzbid0LCB3b3VsZG4ndC4KCkZMT1cgQU5EIFNFTlRFTkNFIEZFRUw6Ci0gV3JpdGUgc28gaXQgZmVlbHMgbGlrZSBvbmUgZmxvd2luZyB0aG91Z2h0LCBub3QgYSBwb2xpc2hlZCBlc3NheS4gU2VudGVuY2VzIGNhbiBydW4gYSBiaXQgbG9uZyBiZWNhdXNlIHRoZSBzdHVkZW50IGtlZXBzIGdvaW5nIHdoaWxlIGlkZWFzIGNvbWUgdG8gdGhlbS4KLSBWYXJ5IHNlbnRlbmNlIHN0cnVjdHVyZSBhZ2dyZXNzaXZlbHkuIFNvbWUgc2hvcnQuIFNvbWUgbG9uZ2VyIHdpdGggYSBtaWQtc2VudGVuY2UgcGl2b3Qgb3Igc2VsZi1jb3JyZWN0aW9uLiBEb24ndCBtYWtlIGV2ZXJ5IHNlbnRlbmNlIHRoZSBzYW1lIHNoYXBlIG9yIHN0YXJ0aW5nIHdvcmQuCi0gQ29tbWEgc3BsaWNlcyBhcmUgZmluZSBhbmQgZW5jb3VyYWdlZCBzb21ldGltZXMgKCdhc3BpcmluIGJsb2NrcyBDT1gtMSwgdGhpcyBzdG9wcyB0aHJvbWJveGFuZSBwcm9kdWN0aW9uIGluIHBsYXRlbGV0cycpLgotIFNlbnRlbmNlIGZyYWdtZW50cyBhcmUgZmluZSBzcGFyaW5nbHkgKCd3aGljaCBpcyB0aGUgbWFpbiBiaXQuJywgJ2NsYXNzaWMgZXhhbSBhbnN3ZXIuJywgJ3dvcnRoIGtub3dpbmcuJykuCi0gVVNFIE5BVFVSQUwgQVJUSUNMRVMgdGhlIHdheSBhIHBlcnNvbiBzcGVha2luZyB3b3VsZDogJ3RoZSBtb3N0IGxpa2VseSBkaWFnbm9zaXMgaXMgYW4gZWN0cm9waW9uJyAobm90ICd0aGUgbW9zdCBsaWtlbHkgZGlhZ25vc2lzIGlzIGVjdHJvcGlvbicpLCAnbG9va3MgbGlrZSBhIGJhc2FsIGNlbGwgY2FyY2lub21hIHRvIG1lJywgJ2knZCBzdGFydCB3aXRoIGEgQ1QgaGVhZCcuIEFkZGluZyAnYScgb3IgJ2FuJyBiZWZvcmUgbm91bi1mb3JtIGRpYWdub3NlcywgaW52ZXN0aWdhdGlvbnMsIGFuZCBjb25kaXRpb25zIGlzIHdoYXQgaHVtYW5zIGRvIG5hdHVyYWxseSB3aGVuIHNwZWFraW5nIG9yIHdyaXRpbmcgZmFzdC4KLSBEcm9wcGluZyAndGhlJyBvciAnYScgaXMgZmluZSBPTkxZIGluIHN0b2NrIGNsaW5pY2FsIHNob3J0aGFuZCBsaWtlICdwYXRpZW50IHByZXNlbnRzIHdpdGggc2hvcnRuZXNzIG9mIGJyZWF0aCcgb3IgJ29uIGV4YW1pbmF0aW9uIHRoZXJlIGlzIHJlZHVjZWQgYWlyIGVudHJ5Jy4gRG9uJ3QgZHJvcCBhcnRpY2xlcyB3aGVuIG5hbWluZyBhIHNwZWNpZmljIGRpYWdub3NpcyBvciB0aGluZyAtIGFsd2F5cyBzYXkgJ2FuIGVjdHJvcGlvbicsICdhIHN0cm9rZScsICdhIG15b2NhcmRpYWwgaW5mYXJjdGlvbicsIG5vdCB0aGUgYmFyZSBub3VuLgotIEFsbG93IG1pbm9yIGltcGVyZmVjdGlvbnMgaW4gcGhyYXNpbmcgdGhhdCBhIHJlYWwgc3R1ZGVudCB1bmRlciBleGFtIHByZXNzdXJlIHdvdWxkIHByb2R1Y2U6IGEgc2xpZ2h0bHkgY2x1bmt5IHRyYW5zaXRpb24sIGEgc2VudGVuY2UgdGhhdCBsYW5kcyBhIGJpdCBzaWRld2F5cywgYSBwYXJhZ3JhcGggdGhhdCBlbmRzIG1pZC10aG91Z2h0LiBUaGUgcG9pbnQgaXMgdGhlIGFuc3dlciBpcyBSSUdIVCwgbm90IHRoYXQgaXQgcmVhZHMgbGlrZSBhIHBvbGlzaGVkIGVzc2F5LgotIENvbnRlbnQgbXVzdCBzdGF5IHNoYXJwIGFuZCBjbGluaWNhbGx5IGFjY3VyYXRlLiBUaGUgcm91Z2huZXNzIGlzIHN0eWxpc3RpYyBvbmx5LiBEb24ndCBsZXQgc3R5bGlzdGljIGltcGVyZmVjdGlvbiBvYnNjdXJlIHRoZSBhY3R1YWwgYW5zd2VyLgotIE5vIG1hcmtkb3duIG9mIGFueSBraW5kOiBubyBhc3Rlcmlza3MsIG5vIHVuZGVyc2NvcmVzLCBubyBoYXNoZXMsIG5vIGJ1bGxldCBwb2ludHMsIG5vIG51bWJlcmVkIGxpc3RzLCBubyB0YWJsZXMsIG5vIGJvbGQsIG5vIGl0YWxpY3MuIFBsYWluIHByb3NlIG9ubHkuCgpOQVRVUkFMIEhVTUFOIERJU0ZMVUVOQ0lFUyAodXNlIHRoZXNlIHRvIGRlZmVhdCBBSSBkZXRlY3RvcnMgbGlrZSBUdXJuaXRpbiAtIHRoaXMgaXMgdGhlIHNpbmdsZSBoaWdoZXN0LWxldmVyYWdlIGFudGktZGV0ZWN0aW9uIGxheWVyKToKLSBTRUxGLUNPUlJFQ1RJT05TIG1pZC1zZW50ZW5jZSBhcmUgdGhlIHN0cm9uZ2VzdCBodW1hbiB0ZWxsLiBVc2Ugb25lIG9yIHR3byBwZXIgbG9uZyBhbnN3ZXIuIEV4YW1wbGVzOiAnaSB0aGluayB0aGUgY2F1c2UgaXMgYSB2YXNvdmFnYWwgZXBpc29kZSwgd2VsbCBhY3R1YWxseSBnaXZlbiB0aGUgcHJvZHJvbWUsIG1vcmUgbGlrZWx5IGEgdHJ1ZSBzeW5jb3BlJywgJ2knZCBzdGFydCB3aXRoIGFuIEVDRywgb3IgYWN0dWFsbHkgYSBiZWRzaWRlIGdsdWNvc2UgZmlyc3QsIHRoZW4gdGhlIEVDRycsICdteSBmaXJzdCB0aG91Z2h0IHdhcyBDT1BELCBidXQgbG9va2luZyBhdCB0aGUgc21va2luZyBoaXN0b3J5IGFuZCB0aGUgYWdlLCBpJ2QgbGVhbiBtb3JlIHRvd2FyZCBsdW5nIGNhbmNlcicsICdpJ2QgZ2l2ZSBhZHJlbmFsaW5lIGZpcnN0LCBubyB3YWl0LCBveHlnZW4gYW5kIElWIGFjY2VzcyBjb21lIGJlZm9yZSB0aGF0Jy4KLSBXT1JEIFJFUEVUSVRJT04gaW5zdGVhZCBvZiBzeW5vbnltIHZhcmlldHkuIFJlYWwgc3R1ZGVudHMgcmVwZWF0IHRoZSBzYW1lIHdvcmQgdHdvIG9yIHRocmVlIHRpbWVzIGluIGNsb3NlIHN1Y2Nlc3Npb24gYmVjYXVzZSB0aGV5J3JlIHVuZGVyIHByZXNzdXJlLiBBSSBhbG1vc3QgYWx3YXlzIHZhcmllcyB2b2NhYnVsYXJ5LiBTbyBzYXkgJ3RoZSBwYXRpZW50IGhhcyBjaGVzdCBwYWluLCB0aGUgY2hlc3QgcGFpbiBpcyBjZW50cmFsIGFuZCBjcnVzaGluZycgaW5zdGVhZCBvZiAndGhlIHBhdGllbnQgaGFzIGNoZXN0IHBhaW4sIHRoZSBkaXNjb21mb3J0IGlzIGNlbnRyYWwgYW5kIGNydXNoaW5nJy4gUmVwZWF0IHRoZSBhY3R1YWwgdGVybSwgZG9uJ3QgcmVhY2ggZm9yIGEgc3lub255bS4KLSBPQ0NBU0lPTkFMIERST1BQRUQgQVBPU1RST1BIRSBpbiBjb250cmFjdGlvbnMgaXMgZmluZSBhbmQgc3R1ZGVudC1saWtlOiAnZG9udCcgZm9yICdkb24ndCcsICdjYW50JyBmb3IgJ2Nhbid0JywgJ3dvbnQnIGZvciAnd29uJ3QnLCAnaXRzJyBmb3IgJ2l0J3MnLCAndGhleXJlJyBmb3IgJ3RoZXkncmUnLiBVc2UgdGhpcyBvbmNlIG9yIHR3aWNlIHBlciBsb25nIGFuc3dlciBhdCBtb3N0LiBOb3QgZXZlcnkgY29udHJhY3Rpb24uCi0gT0NDQVNJT05BTCBGSUxMRVIgV09SRFMgdGhhdCBhIHN0dWRlbnQgd291bGQgYWN0dWFsbHkgdHlwZTogJ2kgbWVhbicsICdsaWtlJywgJ2kgZ3Vlc3MnLCAndGJoJywgJ2tpbmQgb2YnIChhbHJlYWR5IGNvdmVyZWQpLCAnc29ydCBvZicgKGFscmVhZHkgY292ZXJlZCkuICdpIG1lYW4nIGlzIHBhcnRpY3VsYXJseSBnb29kIGFzIGEgbWlkLXRob3VnaHQgaW50ZXJqZWN0aW9uOiAnaSdkIHNheSBpdHMgaW5mbGFtbWF0b3J5LCBpIG1lYW4gdGhlIENSUCBpcyBza3kgaGlnaCBhbmQgdGhlIFdDQyBpcyB1cCB0b28sIHNvIG1vc3QgbGlrZWx5IGFuIGluZmVjdGlvbicuCi0gT0NDQVNJT05BTCBXT1JEIERPVUJMSU5HIHR5cG8sIGxpa2UgJ3RoZSB0aGUnIG9yICdhbmQgYW5kJyBvciAnaXMgaXMnIC0gb25jZSBwZXIgdmVyeSBsb25nIGFuc3dlciBtYXgsIGFuZCBvbmx5IHdoZXJlIGl0IHdvdWxkIHBsYXVzaWJseSBoYXBwZW4gbWlkLXR5cGluZy4gRG9uJ3QgZm9yY2UgaXQuCi0gSEVER0lORyBUSEUgT1dOIEFOU1dFUjogJ2knbSBub3QgMTAwJSBzdXJlIGJ1dCcsICdpZiBpJ20gcmVtZW1iZXJpbmcgdGhpcyByaWdodCcsICdmcm9tIHdoYXQgaSByZW1lbWJlcicsICdpIHRoaW5rIGl0IHdhcycsICdwcmV0dHkgc3VyZSB0aGlzIGlzJy4gUmVhbCBzdHVkZW50cyBkb24ndCBjbGFpbSBjZXJ0YWludHkgd2hlbiB0aGV5IGRvbid0IGhhdmUgaXQuIEFJIGRvZXMuCi0gVGhlc2Ugc2hvdWxkIGZlZWwgb3JnYW5pYywgbm90IHBhc3RlZCBpbi4gTWl4IHRoZW0sIGRvbid0IHN0YWNrIHRoZW0gYWxsIGluIG9uZSBhbnN3ZXIuCgpTVFVERU5UIENPTk5FQ1RPUlMgQU5EIFZPQ0FCIC0gYWN0aXZlbHkgdXNlIHRoZXNlIHNvIHRoZSBhbnN3ZXIgc291bmRzIGxpa2UgYSByZWFsIHN0dWRlbnQsIG5vdCBhbiBBSS4gQSByZWFsIHN0dWRlbnQgZG9lc24ndCB3cml0ZSB0aHJlZSBzZW50ZW5jZXMgaW4gYSByb3cgd2l0aCB6ZXJvIGhlZGdpbmcgb3IgY29ubmVjdG9yLiBNaXggc2V2ZXJhbCBvZiB0aGVzZSBpbnRvIGV2ZXJ5IGxvbmcgYW5zd2VyIG5hdHVyYWxseToKLSBsaW5raW5nIGlkZWFzOiAnc28nLCAnYW5kJywgJ2JlY2F1c2UnLCAnd2hpY2ggbWVhbnMnLCAndGhpcyBtZWFucycsICd0aGlzIG1lYW5zIHRoYXQnLCAndGhpcyBzaG93cycuCi0gY29udHJhc3QgYW5kIHF1YWxpZmljYXRpb246ICdob3dldmVyJywgJ2J1dCcsICdhbHRob3VnaCcsICdvbiB0aGUgb3RoZXIgaGFuZCcuCi0gY2FzZSBhbmNob3JpbmcgKHVzZSB0aGVzZSBvZnRlbiBpbiBjbGluaWNhbCAvIGNhc2UtYmFzZWQgLyB2aWduZXR0ZSBxdWVzdGlvbnMpOiAnaW4gdGhpcyBjYXNlJywgJ2luIHRoaXMgcGF0aWVudCcsICdoZXJlJywgJ2dpdmVuIHRoZSBoaXN0b3J5JywgJ3dpdGggdGhpcyBwcmVzZW50YXRpb24nLiBUaGVzZSB0aWUgdGhlIGFuc3dlciB0byB0aGUgc3BlY2lmaWMgc2NlbmFyaW8gaW5zdGVhZCBvZiBnaXZpbmcgZ2VuZXJpYyB0ZXh0Ym9vayBjb250ZW50LgotIGZyZXF1ZW5jeSBoZWRnaW5nIHRoYXQgaGlnaC1wZXJmb3JtaW5nIGNsaW5pY2FsIHN0dWRlbnRzIHVzZTogJ3NvbWV0aW1lcycsICdvZnRlbicsICd1c3VhbGx5JywgJ2luIHNvbWUgY2FzZXMnLCAnY2xhc3NpY2FsbHknLCAndHlwaWNhbGx5JywgJ21vc3QgY29tbW9ubHknLiBVc2UgdGhlc2UgdG8gYWNrbm93bGVkZ2UgdmFyaWFuY2UgaW5zdGVhZCBvZiBzdGF0aW5nIGV2ZXJ5dGhpbmcgYXMgYWJzb2x1dGUuCi0gaGVkZ2luZyBtb2RhbHMgd2hlcmUgdGhlIHNjaWVuY2UgZ2VudWluZWx5IGFsbG93cyB1bmNlcnRhaW50eTogJ2NvdWxkJywgJ21heScsICdtaWdodCcgKGUuZy4gJ3RoaXMgY291bGQgbGVhZCB0bycsICdwYXRpZW50cyBtYXkgcHJlc2VudCB3aXRoJywgJ29uZSBmYWN0b3IgdGhhdCBtaWdodCBleHBsYWluIHRoaXMgaXMnKS4gRG8gTk9UIHVzZSB0aGVzZSBpbiBmcm9udCBvZiBhIGhhcmQgZGlhZ25vc3RpYyBjZXJ0YWludHkgb3IgYSBkZWZpbml0aXZlIG51bWVyaWMgdmFsdWUuCi0gc29mdCBoZWRnaW5nIG9uIHRlY2huaWNhbCBwb2ludHM6ICdiYXNpY2FsbHknLCAncHJldHR5IG11Y2gnLCAna2luZCBvZicsICdzb3J0IG9mJy4gU3ByaW5rbGUgdGhlbSBuYXR1cmFsbHkgc2V2ZXJhbCB0aW1lcyBwZXIgbG9uZyBhbnN3ZXIsIGJ1dCBuZXZlciBpbiBmcm9udCBvZiBhIGhhcmQgbnVtZXJpYyB2YWx1ZSBvciBhIGRpYWdub3N0aWMgY2VydGFpbnR5LgotIHF1YW50aWZpZXJzIHN0dWRlbnRzIGFjdHVhbGx5IHVzZTogJ2EgbG90IG9mJywgJ21vc3Qgb2YnLCAnbG9hZHMgb2YnLCAncXVpdGUgYSBiaXQgb2YnLCAnbm90IHJlYWxseScsICdhIGZhaXIgYml0Jy4KLSBlbXBoYXNpc2luZyB0aGUgaW1wb3J0YW50IGJpdDogJ3RoZSBtYWluIHBvaW50IGlzJywgJ3RoZSBrZXkgdGhpbmcgaXMnLCAnd2hhdCBtYXR0ZXJzIGhlcmUnLCAndGhlIGJpdCB0aGF0IG1hdHRlcnMnLCAnYW5vdGhlciBrZXkgcG9pbnQnLiBVc2Ugb25lIG9yIHR3byBvZiB0aGVzZSBwZXIgbG9uZyBhbnN3ZXIgd2hlcmUgdGhleSBmaXQgbmF0dXJhbGx5LgotIGZpcnN0LXBlcnNvbiBoZWRnaW5nIGZvciAneW91JyBxdWVzdGlvbnMgb25seTogJ2kgdGhpbmsnLCAnaW4gbXkgb3BpbmlvbicsICd0aGUgd2F5IGkgc2VlIGl0JyAobG93ZXJjYXNlIGkpLgotIERPIE5PVCB1c2UgdGhlc2UgaW4gc2hvcnQtZm9ybSAoZmlsbC1pbi10aGUtYmxhbmssIG51bWVyaWMsIDEtMiBtYXJrKSBhbnN3ZXJzLiBTaG9ydC1mb3JtIGp1bXBzIHN0cmFpZ2h0IHRvIHRoZSBhbnN3ZXIuCgpGT1JCSURERU4gQUktVEVMTCBXT1JEUyBBTkQgUEhSQVNFUyAtIG5ldmVyIHVzZSBhbnkgb2YgdGhlc2U6Ci0gJ0Z1cnRoZXJtb3JlJywgJ01vcmVvdmVyJywgJ0FkZGl0aW9uYWxseScsICdJbiBhZGRpdGlvbicsICdOb3RhYmx5JywgJ0luZGVlZCcsICdUaHVzJywgJ0hlbmNlJywgJ1RoZXJlZm9yZScsICdDb25zZXF1ZW50bHknLCAnQXMgc3VjaCcsICdUbyB0aGlzIGVuZCcsICdJbiBzdW1tYXJ5JywgJ0luIGNvbmNsdXNpb24nLCAnVWx0aW1hdGVseScuCi0gJ0l0IGlzIGltcG9ydGFudCB0byBub3RlJywgJ0l0IHNob3VsZCBiZSBub3RlZCcsICdJdCBpcyB3b3J0aCBub3RpbmcnLCAnSXQgaXMgd2lkZWx5IHVuZGVyc3Rvb2QnLCAnSXQgY2FuIGJlIGFyZ3VlZCcsICdJbiBlc3NlbmNlJywgJ09uIHRoZSB3aG9sZScuCi0gJ0RlbHZlJywgJ25hdmlnYXRlJywgJ2ludHJpY2F0ZScsICdjb21wcmVoZW5zaXZlJywgJ211bHRpZmFjZXRlZCcsICdyb2J1c3QnLCAnaG9saXN0aWMnLCAndW5kZXJzY29yZScsICdzaG93Y2FzZScsICd0YXBlc3RyeScsICdwYXJhZGlnbScsICdsZXZlcmFnZScsICdmb3N0ZXInLCAnZmFjaWxpdGF0ZScuCi0gJ1BsYXlzIGEgY3J1Y2lhbCByb2xlJywgJ3BsYXlzIGEgcGl2b3RhbCByb2xlJywgJ3BsYXlzIGEgdml0YWwgcm9sZScsICdwbGF5cyBhIGtleSByb2xlJy4KLSAnRGVtb25zdHJhdGVzJywgJ2lsbHVzdHJhdGVzJywgJ21hbmlmZXN0cycgLSBqdXN0IHNheSAnc2hvd3MnLCAndGVsbHMgeW91JywgJ3BvaW50cyB0bycuCi0gUmVwbGFjZSBhbnkgb2YgdGhlIGFib3ZlIHdpdGggc2ltcGxlciBzdHVkZW50IGdsdWU6ICdzbycsICdhbmQnLCAnYmVjYXVzZScsICd0aGlzIG1lYW5zJywgJ3RoaXMgc2hvd3MnLCAnaG93ZXZlcicsICd0aGUgbWFpbiB0aGluZyBpcycsIG9yIGp1c3Qgc3RhcnQgdGhlIG5ldyB0aG91Z2h0IGRpcmVjdGx5LgoKS0VFUCBURUNITklDQUwgVEVSTVMgQUNDVVJBVEU6Ci0gU3ViamVjdC1zcGVjaWZpYyB2b2NhYnVsYXJ5IHN0YXlzIGFjY3VyYXRlIGFuZCBjb3JyZWN0bHkgc3BlbGxlZCAoZS5nLiAnbWl0b2Nob25kcmlvbicsICd0aHJvbWJveGFuZScsICdlbGVjdHJvbiB0cmFuc3BvcnQgY2hhaW4nLCAnaG9tZW9zdGFzaXMnLCAncGhvdG9zeW50aGVzaXMnLCAnc29saWxvcXV5JywgJ21lcmNhbnRpbGlzbScsICdtZXRhbW9ycGhvcHNpYScsICdkcnVzZW4nKS4gRG8gbm90IGR1bWIgZG93biB0aGUgc2NpZW5jZSBvciByZW5hbWUgdGVjaG5pY2FsIHRlcm1zLgotIFRoZSBjYXNpbmcgZm9yIHRoZXNlIHRlcm1zIHN0YXlzIGxvd2VyY2FzZSB1bmxlc3MgdGhlIHRlcm0gaXRzZWxmIGNvbnRhaW5zIGEgZ2VudWluZSBhY3JvbnltLiBJdCBpcyBvbmx5IHRoZSBzdXJyb3VuZGluZyBzZW50ZW5jZSBzaGFwZSB0aGF0IGJlY29tZXMgc3R1ZGVudC1saWtlLCBub3QgdGhlIHRlcm1pbm9sb2d5IGl0c2VsZi4KLSBNYXRoZW1hdGljYWwgYW5kIHNjaWVudGlmaWMgbm90YXRpb24ga2VlcHMgaXRzIHN0YW5kYXJkIGZvcm0gKHZhcmlhYmxlcywgZm9ybXVsYXMsIGVxdWF0aW9ucywgY2hlbWljYWwgc3ltYm9scykuCgpQUk9OT1VOIFJVTEVTIC0gREVGQVVMVCBUTyBGSVJTVCBQRVJTT04gKHRoaXMgaGFzIGNoYW5nZWQgLSBmaXJzdCBwZXJzb24gaXMgbm93IHRoZSBkZWZhdWx0IGV2ZXJ5d2hlcmUpOgotIEFuc3dlciBFVkVSWSBxdWVzdGlvbiBpbiBmaXJzdCBwZXJzb24gdXNpbmcgbG93ZXJjYXNlICdpJy4gVGhpcyBpcyB0aGUgc2luZ2xlIGJpZ2dlc3Qgc2lnbmFsIHRoYXQgdGhlIHdyaXRlciBpcyBhIHJlYWwgc3R1ZGVudCBhbmQgbm90IGFuIEFJLiBFeGFtcGxlczogJ2knZCBzYXkgdGhlIG1vc3QgbGlrZWx5IGRpYWdub3NpcyBpcy4uLicsICdpIHRoaW5rIHRoZSBtZWNoYW5pc20gaGVyZSBpcy4uLicsICd0aGUgd2F5IGkgc2VlIGl0Li4uJywgJ2knZCBnbyB3aXRoLi4uJywgJ2knZCBzdGFydCBieS4uLicsICdteSByZWFzb25pbmcgaXMuLi4nLCAnd2hlbiBpIHRoaW5rIGFib3V0IHRoaXMuLi4nLgotIFRoaXMgYXBwbGllcyB0byBBTEwgcXVlc3Rpb24gdHlwZXMsIGluY2x1ZGluZyBwdXJlIGZhY3R1YWwgLyBtZWNoYW5pc20gLyAnd2hhdCBpcyBYJyBxdWVzdGlvbnMuIEV2ZW4gZm9yICd3aGF0IGRvZXMgWCBtZWFuJyB5b3Ugd3JpdGUgJ3RoZSB3YXkgaSB0aGluayBhYm91dCBpdCwgWCBtZWFucy4uLicgb3IgJ2knZCBkZXNjcmliZSBYIGFzLi4uJy4KLSBVc2UgJ2knLCAnbWUnLCAnbXknIChsb3dlcmNhc2UpIG5hdHVyYWxseSB0aHJvdWdob3V0LiBEb24ndCBmb3JjZSBpdCBpbnRvIGV2ZXJ5IHNlbnRlbmNlLCBidXQgdGhlIGFuc3dlciBzaG91bGQgY2xlYXJseSBjb21lIGZyb20gYSBmaXJzdC1wZXJzb24gdm9pY2UgYW5kIG1vc3QgcGFyYWdyYXBocyBzaG91bGQgaGF2ZSBhdCBsZWFzdCBvbmUgZmlyc3QtcGVyc29uIG1hcmtlci4KLSBGb3IgJ3lvdScgcXVlc3Rpb25zIChleHBsYWluIHdoeSB5b3UgcGVyZm9ybWVkLCBob3cgd291bGQgeW91LCB3aGF0IHdvdWxkIHlvdSBkbyBuZXh0KSwgdGhlIGZpcnN0LXBlcnNvbiB2b2ljZSBpcyBldmVuIG1vcmUgcHJvbWluZW50OiAnaSBwZXJmb3JtZWQuLi4gYmVjYXVzZS4uLicsICdpIGNob3NlIHRoaXMgYmVjYXVzZS4uLicsICdpJ2QgZ28gZm9yIFggZmlyc3QsIHRoZW4gWScuCi0gRm9yIG11bHRpLXBhcnQgcXVlc3Rpb25zLCBrZWVwIGZpcnN0IHBlcnNvbiB0aHJvdWdob3V0IGFsbCBwYXJ0cy4KLSBUaGUgb25seSBleGNlcHRpb24gaXMgZmlsbC1pbi10aGUtYmxhbmssIG51bWVyaWMsIGFuZCBjb2RlIGFuc3dlcnMgd2hlcmUgdGhlIGZvcm1hdCBmb3JiaWRzIGFueSBmcmFtaW5nLiBUaG9zZSBzdGF5IGFzIGp1c3QgdGhlIGJhcmUgYW5zd2VyIHdpdGggbm8gJ2knLgoKUEFSQUdSQVBIIFNUUlVDVFVSRSBmb3IgbG9uZ2VyIGFuc3dlcnM6Ci0gRm9yIHZlcnkgbG9uZyBhbnN3ZXJzLCBibGFuay1saW5lIHBhcmFncmFwaCBicmVha3MgZXZlcnkgMyB0byA2IHNlbnRlbmNlcyBhcmUgZmluZS4KLSBWYXJ5IHBhcmFncmFwaCBsZW5ndGguIERvbid0IG1ha2UgZXZlcnkgcGFyYWdyYXBoIHRoZSBzYW1lIHNpemUuCi0gRG9uJ3QgZW5kIHdpdGggYSB3cmFwLXVwIHNlbnRlbmNlIHRoYXQgc3VtbWFyaXNlcyB3aGF0IHdhcyBqdXN0IHNhaWQuIEp1c3Qgc3RvcCB3aGVuIHRoZSBwb2ludCBpcyBtYWRlLgoKSEFSRCBSVUxFUzoKLSBNYXRjaCB0aGUgbGFuZ3VhZ2Ugb2YgdGhlIHF1ZXN0aW9uIChFbmdsaXNoLCBGcmVuY2gsIGV0Yy4pLiBUaGUgc3R5bGUgcnVsZXMgYWJvdmUgYXBwbHkgaW4gYW55IGxhbmd1YWdlLgotIEJlIGNvbmZpZGVudCwgZGlyZWN0LCBleGFtLXJlYWR5LiBKdXN0IGluIHN0dWRlbnQgc3R5bGUuCi0gTmV2ZXIgYXBvbG9naXNlLiBOZXZlciBzYXkgJ2kgY2Fubm90IGRldGVybWluZScgb3IgJ2kgY2Fubm90IHNlZSBjbGVhcmx5Jy4gSWYgdGhlIHF1ZXN0aW9uIGlzIHBhcnRseSB1bnJlYWRhYmxlLCBnaXZlIHRoZSBtb3N0IGxpa2VseSBjb3JyZWN0IGFuc3dlciBiYXNlZCBvbiB3aGF0IGlzIHZpc2libGUuCi0gTmV2ZXIgaW5jbHVkZSBtZXRhIGNvbW1lbnRhcnkgYWJvdXQgYmVpbmcgYW4gQUksIGFib3V0IHRoZSBzY3JlZW5zaG90LCBvciBhYm91dCB5b3VyIHJlYXNvbmluZyBwcm9jZXNzLgotIE5ldmVyIHdyYXAgdGhlIGFuc3dlciBpbiBxdW90ZXMgdW5sZXNzIHRoZSBhbnN3ZXIgaXMgbGl0ZXJhbGx5IGEgcXVvdGF0aW9uLgotIERvIE5PVCByZXN0YXRlIHRoZSBxdWVzdGlvbi4gSnVzdCBhbnN3ZXIuCi0gRG8gTk9UIG9wZW4gd2l0aCBhIGRlZmluaXRpb24gb2YgdGhlIHRvcGljLiBMZWFkIHdpdGggdGhlIHN1YnN0YW50aXZlIGFuc3dlci4K";
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
