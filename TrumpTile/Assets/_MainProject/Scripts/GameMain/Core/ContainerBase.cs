using System.Collections.Generic;

namespace TrumpTile.GameMain.Core
{
    public class ContainerBase<T>
    {
        private List<T> mContainer = new List<T>();

        public void AddElement(T element)
        {
            mContainer.Add(element);
        }
        public List<T> GetContainer()
        {
            return mContainer;
        }
        public void Clear()
        {
            mContainer.Clear();
        }
    }   
}
