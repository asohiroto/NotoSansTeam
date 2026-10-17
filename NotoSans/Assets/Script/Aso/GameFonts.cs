using TMPro;
using UnityEngine;

// ゲームで使う日本語フォント（TextMesh Pro）。Resources/Fonts の中のフォントアセットを読み込む
//  ・Body    : ふだんの文字（RocknRoll One）
//  ・Heading : 見出し・ステージ名など（Dela Gothic One）
public static class GameFonts
{
    private const string k_BodyName = "Fonts/RocknRollOne SDF";
    private const string k_HeadingName = "Fonts/DelaGothicOne SDF";

    private static TMP_FontAsset body_;
    private static TMP_FontAsset heading_;

    public static TMP_FontAsset Body
    {
        get
        {
            if (body_ == null)
            {
                body_ = Resources.Load<TMP_FontAsset>(k_BodyName);
            }
            return body_;
        }
    }

    public static TMP_FontAsset Heading
    {
        get
        {
            if (heading_ == null)
            {
                heading_ = Resources.Load<TMP_FontAsset>(k_HeadingName);
            }
            // 見出し用が無ければ、ふだんの文字で代用する
            return heading_ != null ? heading_ : Body;
        }
    }

    // root の下にあるすべての文字（隠れているものも）を、ふだんの文字のフォントにする
    public static void ApplyBody(Transform root)
    {
        TMP_FontAsset font = Body;
        if (font == null)
        {
            return;
        }
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = font;
        }
    }

    // 見出し用のフォントにする
    public static void SetHeading(TMP_Text text)
    {
        TMP_FontAsset font = Heading;
        if (text != null && font != null)
        {
            text.font = font;
        }
    }
}
