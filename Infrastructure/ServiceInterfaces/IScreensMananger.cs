//*** Guy Ronen © 2008-2011 ***//
using Infrastructure.ObjectModel.Screens;

namespace Infrastructure.ServiceInterfaces
{
    public interface IScreensMananger
    {
        GameScreen ActiveScreen { get; }
        void SetCurrentScreen(GameScreen i_NewScreen);
        bool Remove(GameScreen i_Screen);
        void Add(GameScreen i_Screen);
    }
}
