// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("uRYKsjKImAUzKXVpMzFXLJXs+5m6no/hfb1w4Iio5/y89pkk83xAL402CoM4+G4wRJkJavjbCLpiW3oLQvBzUEJ/dHtY9Dr0hX9zc3N3cnFxW6p2zDSPKwVUOHfhiJQEywwCDJs3Nbf9DWEGgV0pAd/Lb5dRjOHoVZSTqNQan/Q5WrUwDq8AKkFh6B/wc31yQvBzeHDwc3Nyxz5LJm/iWxGWMmSrIGxMD7ajPH/xYdN7Icod+U3rAIhwCF6X4md3gIOdqLEbWc+d1j7eABfFh63r05N25uJz8IyvzwCWbMbAciN7lgdG+/LzzlkR4IQfDYj7MhF/DcdwrakMwkddaQpd0zN+k0D/D6OZhu9zL7rSNJ74+POW/88M/Sq5Qn2hM3Bxc3Jz");
        private static int[] order = new int[] { 11,10,5,11,4,6,7,10,11,10,12,13,13,13,14 };
        private static int key = 114;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
