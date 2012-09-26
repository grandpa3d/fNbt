﻿using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using LibNbt.Queries;

namespace LibNbt.Tags {
    public class NbtList : NbtTag {
        internal override NbtTagType TagType {
            get { return NbtTagType.List; }
        }

        [NotNull]
        public List<NbtTag> Tags { get; protected set; }

        public NbtTagType ListType { get; protected set; }


        public NbtList()
            : this( null ) {}


        public NbtList( [CanBeNull] string tagName )
            : this( tagName, new NbtTag[0], NbtTagType.Unknown ) {}


        public NbtList( [CanBeNull] string tagName, [NotNull] IEnumerable<NbtTag> tags, NbtTagType listType ) {
            Name = tagName;
            Tags = new List<NbtTag>();
            ListType = listType;

            Tags.AddRange( tags );
            if( Tags.Count > 0 ) {
                if( ListType == NbtTagType.Unknown ) {
                    ListType = Tags[0].TagType;
                }
                foreach( NbtTag tag in Tags ) {
                    if( tag.TagType != ListType ) {
                        throw new ArgumentException( String.Format( "All tags must be of type {0}", ListType ), "tags" );
                    }
                }
            }
        }


        public NbtTag this[ int tagIndex ] {
            get { return Get<NbtTag>( tagIndex ); }
            set { Tags[tagIndex] = value; }
        }


        public NbtTag Get( int tagIndex ) {
            return Get<NbtTag>( tagIndex );
        }


        public T Get<T>( int tagIndex ) where T : NbtTag {
            return (T)Tags[tagIndex];
        }


        public override NbtTag Query( string query ) {
            return Query<NbtTag>( query );
        }


        public override T Query<T>( string query ) {
            var tagQuery = new TagQuery( query );

            return Query<T>( tagQuery );
        }


        internal override T Query<T>( TagQuery query, bool bypassCheck ) {
            TagQueryToken token = query.Next();

            if( !bypassCheck ) {
                if( token != null && !token.Name.Equals( Name ) ) {
                    return null;
                }
            }

            var nextToken = query.Peek();
            if( nextToken != null ) {
                // Make sure this token is an integer because NbtLists don't have
                // named tag items
                int tagIndex;
                if( !int.TryParse( nextToken.Name, out tagIndex ) ) {
                    throw new NbtQueryException(
                        string.Format( "Attempt to query by name on a list tag that doesn't support names. ({0})",
                                       Name ) );
                }

                var indexedTag = Get( tagIndex );
                if( indexedTag == null ) {
                    return null;
                }

                if( query.TokensLeft() > 1 ) {
                    // Pop the index token so the current token is the next
                    // named token to continue the query
                    query.Next();

                    // Bypass the name check because the tag won't have one
                    return indexedTag.Query<T>( query, true );
                }

                return (T)indexedTag;
            }

            return (T)( (NbtTag)this );
        }


        public void SetListType( NbtTagType listType ) {
            foreach( var tag in Tags ) {
                if( tag.TagType != listType ) {
                    throw new Exception( "All list items must be the specified tag type." );
                }
            }
            ListType = listType;
        }


        #region Reading Tag

        internal override void ReadTag( NbtReader readStream, bool readName ) {
            // First read the name of this tag
            if( readName ) {
                Name = readStream.ReadString();
            }

            // read list type, and make sure it's defined
            ListType = readStream.ReadTagType();
            if( !Enum.IsDefined( typeof( NbtTagType ), ListType ) || ListType == NbtTagType.Unknown ) {
                throw new Exception( String.Format( "Unrecognized TAG_List tag type: {0}", ListType ) );
            }

            int length = readStream.ReadInt32();
            if( length < 0 ) {
                throw new Exception( "Negative count given in TAG_List" );
            }

            Tags.Clear();
            for( int i = 0; i < length; i++ ) {
                switch( ListType ) {
                    case NbtTagType.Byte:
                        var nextByte = new NbtByte();
                        nextByte.ReadTag( readStream, false );
                        Tags.Add( nextByte );
                        break;
                    case NbtTagType.Short:
                        var nextShort = new NbtShort();
                        nextShort.ReadTag( readStream, false );
                        Tags.Add( nextShort );
                        break;
                    case NbtTagType.Int:
                        var nextInt = new NbtInt();
                        nextInt.ReadTag( readStream, false );
                        Tags.Add( nextInt );
                        break;
                    case NbtTagType.Long:
                        var nextLong = new NbtLong();
                        nextLong.ReadTag( readStream, false );
                        Tags.Add( nextLong );
                        break;
                    case NbtTagType.Float:
                        var nextFloat = new NbtFloat();
                        nextFloat.ReadTag( readStream, false );
                        Tags.Add( nextFloat );
                        break;
                    case NbtTagType.Double:
                        var nextDouble = new NbtDouble();
                        nextDouble.ReadTag( readStream, false );
                        Tags.Add( nextDouble );
                        break;
                    case NbtTagType.ByteArray:
                        var nextByteArray = new NbtByteArray();
                        nextByteArray.ReadTag( readStream, false );
                        Tags.Add( nextByteArray );
                        break;
                    case NbtTagType.String:
                        var nextString = new NbtString();
                        nextString.ReadTag( readStream, false );
                        Tags.Add( nextString );
                        break;
                    case NbtTagType.List:
                        var nextList = new NbtList();
                        nextList.ReadTag( readStream, false );
                        Tags.Add( nextList );
                        break;
                    case NbtTagType.Compound:
                        var nextCompound = new NbtCompound();
                        nextCompound.ReadTag( readStream, false );
                        Tags.Add( nextCompound );
                        break;
                }
            }
        }

        #endregion


        #region Write Tag

        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.List );
            if( writeName ) {
                if( Name == null ) throw new NullReferenceException( "Name is null" );
                writeStream.Write( Name );
            }
            WriteData( writeStream );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( ListType );
            writeStream.Write( Tags.Count );
            foreach( NbtTag tag in Tags ) {
                tag.WriteData( writeStream );
            }
        }

        #endregion


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_List" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0} entries\n", Tags.Count );

            sb.Append( "{\n" );
            foreach( NbtTag tag in Tags ) {
                sb.AppendFormat( "\t{0}\n", tag.ToString().Replace( "\n", "\n\t" ) );
            }
            sb.Append( "}" );
            return sb.ToString();
        }
    }
}