using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Overlay.Objects
{
    ///<summary>
    /// 단일체 패턴 추상 클래스.
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public abstract class Singleton<T> where T : class
    {
        ///<summary>
        /// 단일체 패턴 내부 정적 인스턴스
        ///</summary>
        private static volatile T instance = null;
        ///<summary>
        /// 단일체 패턴 생성자
        ///</summary>
        protected Singleton()
        {
        }
        ///<summary>
        /// 인스턴스에 접근 하기 위한 속성 필드
        ///</summary>
        public static T Ins
        {
            get
            {
                if (instance == null)
                {
                    lock (typeof(T))
                    {
                        if (instance == null)
                        {
                            CreateInstance();
                        }
                    }
                }
                return instance;
            }
        }
        ///<summary>
        /// 단일체 패턴의 인스턴스를 생성하는 메소드.
        ///</summary>
        private static void CreateInstance()
        {
            Type type = typeof(T);
            ConstructorInfo[] ctors = type.GetConstructors();
            if (ctors.Length > 0)
                throw new InvalidOperationException(string.Format("{0} 타입은 현재 단일체 패턴에 위배되는 생성자를 가지고 있습니다.", type.Name));
            instance = (T)Activator.CreateInstance(type, true);
        }
    }
}
