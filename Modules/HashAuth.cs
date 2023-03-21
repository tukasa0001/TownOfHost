using System.Security.Cryptography;
using System.Text;

namespace TOHE;

public class HashAuth
{
    public readonly string HashValue;

    private readonly string salt;
    private HashAlgorithm algorithm;
    public HashAuth(string hashValue, string salt = null, HashAlgorithm algorithm = null)
    {
        HashValue = hashValue;
        this.salt = salt;
        this.algorithm = algorithm ?? SHA256.Create(); // 引数のalgorithmがnullの場合のみ新しく作る
    }

    public bool CheckString(string value)
    {
        var hash = CalculateHash(value);
        return HashValue == hash;
    }
    public string CalculateHash(string source)
        => HashAuth.CalculateHash(source, salt, algorithm);

    public static string CalculateHash(string source, string salt = null, HashAlgorithm algorithm = null)
    {
        // 0.algorithmの初期化
        algorithm ??= SHA256.Create();

        // 1.saltの適用
        if (salt != null) source += salt;

        // 2.sourceをbyte配列に変換
        var sourceBytes = Encoding.UTF8.GetBytes(source);

        // 3.sourceBytesをハッシュ化
        var hashBytes = algorithm.ComputeHash(sourceBytes);

        // 4.hashBytesを文字列化
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
            sb.Append(b.ToString("x2")); //1byteずつ2桁の16進法表記に変換する

        return sb.ToString();
    }

    // Hash値確認用 Hash化してからインスタンスを生成
    // あくまでHash値の確認と動作テストを同時に行うためのものです。確認後は使用しないでください。
    public static HashAuth CreateByUnhashedValue(string value, string salt = null)
    {
        // 1.ハッシュ値計算
        var algorithm = SHA256.Create();
        string hashValue = CalculateHash(value, salt, algorithm);

        // 2.ハッシュ値のログ出力
        //  salt有: ハッシュ値算出結果:<value> => <hashValue> (salt: <saltValue>)
        //  salt無: ハッシュ値算出結果:<value> => <hashValue>
        Logger.Info($"ハッシュ値算出結果: {value} => {hashValue} {(salt == null ? "" : $"(salt: {salt})")}", "HashAuth");
        Logger.Warn("以上の値をソースコード上にペーストしてください。", "HashAuth");

        // 3.HashAuthインスタンスの生成・リターン
        return new HashAuth(hashValue, salt, algorithm);
    }
}