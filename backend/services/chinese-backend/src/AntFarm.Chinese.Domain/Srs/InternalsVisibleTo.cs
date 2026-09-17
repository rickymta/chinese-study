using System.Runtime.CompilerServices;

// Cho phép AntFarm.Chinese.UnitTests kiểm trực tiếp vài công thức FSRS thuần (I(S), D0 — §5.2.7
// V7) mà không cần lách qua máy trạng thái Review()/Preview(), tránh phải mở rộng bề mặt API
// công khai của FsrsScheduler ra ngoài đúng hợp đồng §5.2.6.
[assembly: InternalsVisibleTo("AntFarm.Chinese.UnitTests")]
