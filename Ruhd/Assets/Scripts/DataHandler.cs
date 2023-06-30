using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class DataHandler : MonoBehaviour
{
    const string numbersDataPath = "Data/Numbers";
    const string coloursDataPath = "Data/Colours";
    const string imagesDataPath = "Data/Images";
    const string tilesImportPath = "Assets/Resources/Tiles";

    static List<TileData> cachedCards;

    public static List<TileData> GetAllCards()
    {
        if( cachedCards == null )
        {
            var cards = Resources.LoadAll<TileData>( "Tiles" );
            cachedCards = cards.ToList();
        }
        return cachedCards;
    }

    [MenuItem( "Ruhd/Import Tiles" )]
    static void ImportTiles()
    {
        var numbers = Resources.Load<TextAsset>( numbersDataPath );
        var colours = Resources.Load<TextAsset>( coloursDataPath );
        var images = Resources.Load<TextAsset>( imagesDataPath );
        int idx = 0;
        var zippedData = Utility.Zip( numbers.text.Split( '\n' ), colours.text.Split( '\n' ), images.text.Split( '\n' ) );

        foreach( var( number, colour, image ) in zippedData )
        {
            var cardNumbers = number.Split( ',' );
            var cardColours = colour.Split( ',' );
            var newCard = ScriptableObject.CreateInstance<TileData>();
            newCard.imagePath = image.Trim();

            for( int i = 0; i < Utility.GetNumEnumValues<Side>(); ++i )
            {
                newCard.sides[i] = new TileSide( newCard, int.Parse( cardNumbers[i].Trim() ), int.Parse( cardColours[i].Trim() ) );
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