using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics; 

/*
 * this is a interface that all screens in the game will use
 */
namespace DegreeQuest
{
    class GameScreen
    {
        protected ContentManager content;

        public virtual void Initialize() { }
        public virtual void LoadContent(ContentManager c)
        {
            content = new ContentManager(c.ServiceProvider, "Content");
        }
        public virtual void UnloadContent()
        {
            content.Unload();
        }
        public virtual void Draw(SpriteBatch sb) { }
        public virtual void Update(GameTime gt) { }
    }
}
