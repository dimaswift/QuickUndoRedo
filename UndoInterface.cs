using System.Collections.Generic;

namespace QuickUndoRedo
{
    public interface IUndoFactory
    {
        IUndoable Get(int id);
        IEnumerable<IUndoable> GetAll();
        bool HasKey(int key);
        IUndoable Create(string path);
        IUndoable CreateWithID(string path, int id);
        void Delete(IUndoable undoable);
    }

    public interface IUndoable
    {
        IUndoableState SaveState();
        void LoadState(IUndoableState state);
        bool isDirty { get; set; }
        bool isJustCreated { get; set; }
    }

    public interface IUndoableState
    {
        int ID { get; }
        string sourcePath { get; }
    }
}
