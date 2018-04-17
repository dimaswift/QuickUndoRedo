using System.Collections.Generic;
using System.Diagnostics;

namespace QuickUndoRedo.Example
{
    public class UndoSingleton
    {
        public static event System.Action onUndoRedoPerformed;

        Core.Undo quickUndo;

        static UndoSingleton _instance;

        static UndoSingleton Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new UndoSingleton(100, new BookFactory(new Dictionary<string, string>()));
                return _instance;
            }
        }

        public static void Init(int capacity, BookFactory factory)
        {
            _instance = new UndoSingleton(capacity, factory);
        }

        public UndoSingleton(int capacity, BookFactory factory)
        {
            quickUndo = new Core.Undo(factory, capacity, capacity);
            quickUndo.onUndoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            if (onUndoRedoPerformed != null)
                onUndoRedoPerformed();
        }

        public static void Record()
        {
            Instance.quickUndo.Record();
        }

        public static void PerformUndo()
        {
            Instance.quickUndo.PerformUndo();
        }

        public static void PerormRedo()
        {
            Instance.quickUndo.PerformRedo();
        }
    }


    public class BookFactory : IUndoFactory
    {
        Dictionary<int, Book> printedBooks;
        Dictionary<string, string> library;

        int idCounter;

        public BookFactory(Dictionary<string, string> library)
        {
            printedBooks = new Dictionary<int, Book>();
            this.library = library;
        }

        public Book PrintBook(string libraryID)
        {
            return PrintBook(libraryID, idCounter++);
        }

        public Book PrintBook(string libraryID, int id)
        {
            var book = new Book()
            {
                text = library[libraryID],
                ID = id,
                source = libraryID
            };
            book.isJustCreated = true;
            if (printedBooks.ContainsKey(id)) printedBooks[id] = book;
            else printedBooks.Add(id, book);
            return book;
        }

        public Book GetBook(int id)
        {
            return printedBooks.ContainsKey(id) ? printedBooks[id] : null;
        }

        public void DeleteBook(Book book)
        {
            printedBooks.Remove(book.ID);
        }

        IUndoable IUndoFactory.Create(string path)
        {
            return PrintBook(path);
        }

        IUndoable IUndoFactory.CreateWithID(string path, int id)
        {
            return PrintBook(path, id);
        }

        void IUndoFactory.Delete(IUndoable undoable)
        {
            DeleteBook(undoable as Book);
        }

        IUndoable IUndoFactory.Get(int id)
        {
            return GetBook(id);
        }

        IEnumerable<IUndoable> IUndoFactory.GetAll()
        {
            foreach (var b in printedBooks)
            {
                yield return b.Value;
            }
        }

        bool IUndoFactory.HasKey(int key)
        {
            return printedBooks.ContainsKey(key);
        }
    }

    public class Book : IUndoable
    {
        public string text;
        public int ID { get; set; }
        public bool isDirty { get; set; }
        public bool isJustCreated { get; set; }
        public string source;

        public void Load(BookContent content)
        {
            text = content.text;
            ID = content.ID;
            source = content.sourcePath;
        }

        public BookContent SaveContent()
        {
            return new BookContent()
            {
                ID = ID,
                text = text,
                sourcePath = source
            };
        }

        void IUndoable.LoadState(IUndoableState state)
        {
            Load(state as BookContent);
        }

        IUndoableState IUndoable.SaveState()
        {
            return SaveContent();
        }
    }

    public class BookContent : IUndoableState
    {
        public string text;

        public int ID { get; set; }

        public string sourcePath { get; set; }
    }

    public class PageUndoTest
    {
        public PageUndoTest()
        {
            var library = new Dictionary<string, string>() // just a sample library of books
            {
                { "poem", "Roses are red, violets are blue.." },
                { "tale", "Once upon a time..." }
            };

            var bookFactory = new BookFactory(library); // creates book factory using above library

            var undo = new Core.Undo(bookFactory, 10, 10); // creates undo object with specified factory and capacity

            var poem = bookFactory.PrintBook("poem"); // creates new book

            var tale = bookFactory.PrintBook("tale"); // creates new book

            var poemBookId = poem.ID; // stores bookID assigned by factory

            undo.Record(); // records undo snapshot with just created poem book. always record after creating a book

            undo.PerformUndo(); // just created book deleted from factory

            Debug.Assert(bookFactory.GetBook(poemBookId) == null); // book is not in the factory anymore, but it's still referenced by undo object

            undo.PerformRedo(); // new book is created from factory with the poem state

            poem = bookFactory.GetBook(poemBookId);

            poem.isDirty = true;  // modifying book and marking it as dirty, so undo could save it's state

            undo.Record(); // records undo snapshot with all modified books marked as 'dirty'

            poem.text += ".. I hate coding, and so do you";

            undo.PerformUndo(); // sets poem back to original

            Debug.Assert(poem.text == "Roses are red, violets are blue..");

            undo.PerformRedo(); // sets poem content back to modified earlier state

            poem.isDirty = true; // marking the book dirty before deleting (!!!)

            undo.Record(); // records snapshot before deleting book 

            bookFactory.DeleteBook(poem); // deletes book from factory

            undo.PerformUndo(); // bring deleted book back to factory

            poem = bookFactory.GetBook(poemBookId); // book is in the factory

            Debug.Assert(poem.text == "Roses are red, violets are blue.... I hate coding, and so do you");

        }
    }

}
