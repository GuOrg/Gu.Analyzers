namespace ValidCode
{
    using System;
    using System.IO;

    internal class Exceptions
    {
        private readonly string path;

        public Exceptions(string text)
        {
            this.path = text ?? throw new System.ArgumentNullException(nameof(text));
        }

        public void M()
        {
            try
            {
                _ = File.ReadAllText(this.path);
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e);
                throw;
            }

            try
            {
                _ = File.ReadAllText(this.path);
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            try
            {
                _ = File.ReadAllText(this.path);
            }
            catch
            {
                throw;
            }
        }
    }
}
