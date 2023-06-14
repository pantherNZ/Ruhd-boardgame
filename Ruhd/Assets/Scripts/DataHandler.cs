using UnityEditor;
using UnityEngine;

public class DataHandler : MonoBehaviour
{
    const string numbersDataPath = "Data/Numbers";
    const string coloursDataPath = "Data/Colours";
    const string tilesImportPath = "Assets/Resources/Tiles";

    [MenuItem( "Ruhd/Import Tiles" )]
    static void ImportTiles()
    {
        var numbers = Resources.Load<TextAsset>( numbersDataPath );
        var colours = Resources.Load<TextAsset>( coloursDataPath );
        int idx = 0;

        foreach( var( number, colour ) in Utility.Zip( numbers.text.Split('\n'), colours.text.Split( '\n' ) ) )
        {
            var cardNumbers = number.Split( ',' );
            var cardColours = colour.Split( ',' );
            var newCard = ScriptableObject.CreateInstance<CardData>();

            for( int i = 0; i < ( int )Side.NumSides; ++i )
            {
                newCard.sides[i] = new CardSide( int.Parse( cardNumbers[i].Trim() ), int.Parse( cardColours[i].Trim() ) );
            }

            AssetDatabase.CreateAsset( newCard, tilesImportPath + "/Tile" + idx + ".asset" );
            ++idx;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
    }

    [MenuItem( "Ruhd/Destroy Tiles" )]
    static void DestroyTiles()
    {
        foreach( var asset in AssetDatabase.FindAssets( "", new string[]{ tilesImportPath } ) )
        {
            var path = AssetDatabase.GUIDToAssetPath( asset );
            AssetDatabase.DeleteAsset( path );
        }
    }
}