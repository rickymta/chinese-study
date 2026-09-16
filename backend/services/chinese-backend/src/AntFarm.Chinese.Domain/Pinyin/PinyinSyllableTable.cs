namespace AntFarm.Chinese.Domain.Pinyin;

/// <summary>
/// Danh sách ĐẦY ĐỦ 406 âm tiết pinyin (không thanh) — sao y nội dung
/// <c>content/chinese/data/pinyin/syllables.json</c> (F5) dưới dạng hằng số Domain, KHÔNG đọc file.
///
/// LÝ DO cần bảng này (ĐIỂM LỆCH so với §5.2.1 hợp đồng F6/F7 — đã báo lại ở bàn giao F6.2):
/// hợp đồng mô tả <c>PinyinQuery.Segment</c> "dùng <see cref="PinyinSyllable.IsValidKey"/>" để tách
/// ranh giới âm tiết, nhưng <c>IsValidKey</c> (R5-1, F5) CHỈ kiểm ĐỊNH DẠNG ký tự (chữ thường,
/// hạn chế dùng 'v') chứ KHÔNG kiểm âm tiết có thật hay không — "nihao" hay "abc" đều "qua" được
/// <c>IsValidKey</c>. Không có bảng tra âm tiết THẬT thì không thể tách "nihao" thành "ni"+"hao"
/// một cách xác định (nhiều cách tách đều "hợp lệ" theo <c>IsValidKey</c>, kể cả tách sai) — đã kiểm
/// chứng bằng tay với ví dụ "xīān" (西安, 2 âm tiết) so với "xiān" (先, 1 âm tiết dùng đúng ký tự
/// pinyin): chỉ có bảng âm tiết thật + số dấu thanh trong mỗi âm tiết mới phân biệt được.
///
/// Trùng dữ liệu với <c>syllables.json</c> là CÓ CHỦ Ý: Domain không được phụ thuộc Infrastructure
/// (đọc file học liệu) theo kiến trúc DDD 4 lớp; đây là bảng ngữ âm tiếng Trung Quốc CHUẨN, ổn định
/// (không phải học liệu do biên tập viên chỉnh sửa), nên nhúng cứng chấp nhận được.
/// </summary>
public static class PinyinSyllableTable
{
    public static readonly IReadOnlySet<string> Keys = new HashSet<string>(StringComparer.Ordinal)
    {
        "a", "ai", "an", "ang", "ao", "ba", "bai", "ban", "bang", "bao",
        "bei", "ben", "beng", "bi", "bian", "biao", "bie", "bin", "bing", "bo",
        "bu", "ca", "cai", "can", "cang", "cao", "ce", "cen", "ceng", "cha",
        "chai", "chan", "chang", "chao", "che", "chen", "cheng", "chi", "chong", "chou",
        "chu", "chuai", "chuan", "chuang", "chui", "chun", "chuo", "ci", "cong", "cou",
        "cu", "cuan", "cui", "cun", "cuo", "da", "dai", "dan", "dang", "dao",
        "de", "dei", "den", "deng", "di", "dia", "dian", "diao", "die", "ding",
        "diu", "dong", "dou", "du", "duan", "dui", "dun", "duo", "e", "ei",
        "en", "er", "fa", "fan", "fang", "fei", "fen", "feng", "fo", "fou",
        "fu", "ga", "gai", "gan", "gang", "gao", "ge", "gei", "gen", "geng",
        "gong", "gou", "gu", "gua", "guai", "guan", "guang", "gui", "gun", "guo",
        "ha", "hai", "han", "hang", "hao", "he", "hei", "hen", "heng", "hong",
        "hou", "hu", "hua", "huai", "huan", "huang", "hui", "hun", "huo", "ji",
        "jia", "jian", "jiang", "jiao", "jie", "jin", "jing", "jiong", "jiu", "ju",
        "juan", "jue", "jun", "ka", "kai", "kan", "kang", "kao", "ke", "kei",
        "ken", "keng", "kong", "kou", "ku", "kua", "kuai", "kuan", "kuang", "kui",
        "kun", "kuo", "la", "lai", "lan", "lang", "lao", "le", "lei", "leng",
        "li", "lia", "lian", "liang", "liao", "lie", "lin", "ling", "liu", "lo",
        "long", "lou", "lu", "luan", "lun", "luo", "lv", "lve", "ma", "mai",
        "man", "mang", "mao", "me", "mei", "men", "meng", "mi", "mian", "miao",
        "mie", "min", "ming", "miu", "mo", "mou", "mu", "na", "nai", "nan",
        "nang", "nao", "ne", "nei", "nen", "neng", "ni", "nian", "niang", "niao",
        "nie", "nin", "ning", "niu", "nong", "nou", "nu", "nuan", "nuo", "nv",
        "nve", "o", "ou", "pa", "pai", "pan", "pang", "pao", "pei", "pen",
        "peng", "pi", "pian", "piao", "pie", "pin", "ping", "po", "pou", "pu",
        "qi", "qia", "qian", "qiang", "qiao", "qie", "qin", "qing", "qiong", "qiu",
        "qu", "quan", "que", "qun", "ran", "rang", "rao", "re", "ren", "reng",
        "ri", "rong", "rou", "ru", "ruan", "rui", "run", "ruo", "sa", "sai",
        "san", "sang", "sao", "se", "sen", "seng", "sha", "shai", "shan", "shang",
        "shao", "she", "shei", "shen", "sheng", "shi", "shou", "shu", "shua", "shuai",
        "shuan", "shuang", "shui", "shun", "shuo", "si", "song", "sou", "su", "suan",
        "sui", "sun", "suo", "ta", "tai", "tan", "tang", "tao", "te", "teng",
        "ti", "tian", "tiao", "tie", "ting", "tong", "tou", "tu", "tuan", "tui",
        "tun", "tuo", "wa", "wai", "wan", "wang", "wei", "wen", "weng", "wo",
        "wu", "xi", "xia", "xian", "xiang", "xiao", "xie", "xin", "xing", "xiong",
        "xiu", "xu", "xuan", "xue", "xun", "ya", "yan", "yang", "yao", "ye",
        "yi", "yin", "ying", "yong", "you", "yu", "yuan", "yue", "yun", "za",
        "zai", "zan", "zang", "zao", "ze", "zei", "zen", "zeng", "zha", "zhai",
        "zhan", "zhang", "zhao", "zhe", "zhei", "zhen", "zheng", "zhi", "zhong", "zhou",
        "zhu", "zhua", "zhuai", "zhuan", "zhuang", "zhui", "zhun", "zhuo", "zi", "zong",
        "zou", "zu", "zuan", "zui", "zun", "zuo"
    };

    /// <summary>Âm tiết có thật (406 âm tiết chuẩn) HOẶC 'r' (hậu tố nhi hoá 儿化, vd "wanr" — không phải âm tiết độc lập nên không có trong bảng).</summary>
    public static bool IsKnown(string key) => Keys.Contains(key) || key == "r";
}
