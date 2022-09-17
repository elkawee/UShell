using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;




using LightJson ;

using UnityEngine;

public class SequenceExpType : ExpType
{
    public ExpType memberType ;
    public SequenceExpType( ExpType memberType )
    {
        this.memberType = memberType;
    }

    public struct override_args_dim1 {  public Type motherT ; public Type childT ; public int len ;};
    public struct override_args_dim2 {  public Type motherT ; public Type childT ; public int len_outer ;public int len_inner ;};

    public static Dictionary<Type , override_args_dim1> const_len_indexer_overrides_dim1 = new Dictionary<Type, override_args_dim1>() ; 
    public static Dictionary<Type , override_args_dim2> const_len_indexer_overrides_dim2 = new Dictionary<Type, override_args_dim2>() ; 

    static SequenceExpType()
    {
        const_len_indexer_overrides_dim1[typeof(Vector2)] = new override_args_dim1 { motherT = typeof(Vector2) , childT = typeof(float) , len = 2 };
        const_len_indexer_overrides_dim1[typeof(Vector3)] = new override_args_dim1 { motherT = typeof(Vector3) , childT = typeof(float) , len = 3 };

        const_len_indexer_overrides_dim2[typeof(UnityEngine.Rendering.SphericalHarmonicsL2)] = 
            new override_args_dim2 { motherT = typeof(UnityEngine.Rendering.SphericalHarmonicsL2) , childT = typeof(float) , len_outer = 2 , len_inner = 8 };

        const_len_indexer_overrides_dim2[typeof(UnityEngine.Matrix4x4)] = 
            new override_args_dim2 { motherT = typeof(UnityEngine.Matrix4x4) , childT = typeof(float) , len_outer = 4 , len_inner = 4 };
    }

    
    // naming deviation to natruallyIs is on purpose - this thing is a bit funky 
    public static bool canonicallyIs( Type csharp_type )
    {
        return csharp_type.IsArray 
                || const_len_indexer_overrides_dim1.ContainsKey(csharp_type)
                || const_len_indexer_overrides_dim2.ContainsKey(csharp_type);
    }

    public static ExpSerializer GetSerializerR( Type csharp_type , bool only_natural = false )
    {
        try {  
            if ( csharp_type.IsArray ) 
            {
                if ( csharp_type.GetArrayRank() == 1) { 
                    var complete_type =  typeof(ExpSerializer_Array_Dim1<>).MakeGenericType( new [] { csharp_type.GetElementType()  } );
                    return (ExpSerializer)Activator.CreateInstance(complete_type , new  object [] { only_natural });
                } else throw new NotImplementedException();

            } else if ( const_len_indexer_overrides_dim1.ContainsKey( csharp_type))
            {
                var type_args = const_len_indexer_overrides_dim1[csharp_type]; 
                var complete_type = typeof ( ExpSerializer_IndexerConstLen<,> ).MakeGenericType( new [] { type_args.motherT , type_args.childT }  );
                return (ExpSerializer) Activator.CreateInstance( complete_type , new object [] { type_args.len , only_natural } );
            } else if ( const_len_indexer_overrides_dim2.ContainsKey( csharp_type  ) )
            {
                var type_args = const_len_indexer_overrides_dim2[csharp_type]; 
                var complete_type = typeof ( ExpSerializer_IndexerConstLen_Dim2<,> ).MakeGenericType( new [] { type_args.motherT , type_args.childT }  );

                return (ExpSerializer) Activator.CreateInstance( complete_type , new object [] { type_args.len_outer , type_args.len_inner , only_natural } );
            }
        } catch ( TargetInvocationException e )
        {
            if (  e.InnerException is NoSerializerForTypeException ) throw e.InnerException; 
            else throw e ; 
        }

        throw new NotImplementedException();
    }

}


/*
    the default ExpSerializer is the only consumer of this interface -> it can be a lot more restricted then usual for data structures 

    Create(int N )   // with a known size in advance , to make it easy for binary serializers to implement 
    Add( val )       // exaclty N times 

    that's it :) 
*/

public interface SequenceBuilder<TMPVAL_Type>
{
    void Append( TMPVAL_Type elem ) ;
    TMPVAL_Type Final();
}


/*
    this doesn't mesh nicely with the 

    public abstract TMPVAL_Type CREATE          ( ExpType          exp_type             );
    public abstract TMPVAL_Type CREATE_Primitive( PrimitiveExpType exp_type , object arg);

    interface 
    i would want to also use a length argument 
    ( restrict the sequence type to represent stuff for which the length of an instance can be known without iterating over it ) 

    this means that sequence types are the only ones for which a CREATE() is not possible from an instance of ExpType allone 

    possible interfaces  
    [ Create              , Add* , Finalize ]       // problem is, this one will need a temporary structure , to hold the elements : slower , more work to implement 
    [ Create ( int Size ) , Add*            ] 

*/

public interface SequenceView_RO<TMPVAL_Type>
{
    int Count();
    TMPVAL_Type Next();
}



public class ExpSerializer_IndexerConstLen<TContainer,TElement>  : ExpSerializer where TContainer : new() 
{
    public int len;
    public ExpSerializer element_serializer;

    public ExpSerializer_IndexerConstLen ( int len , bool only_natural = false )
    {
        this.len = len ;
        element_serializer = ExpType.GetSerializer( typeof(TElement) , only_natural);
        _csharp_type = typeof(TContainer);
        _exp_type    = new SequenceExpType( element_serializer.exp_type);

        setterMI = typeof(TContainer).GetMethod("set_Item" , new [] { typeof(int) , typeof(TElement)} ) ;
        getterMI = typeof(TContainer).GetMethod("get_Item" , new [] { typeof(int) });
    }

    public SequenceExpType _exp_type ;
    public override ExpType exp_type => _exp_type;
    public Type _csharp_type;
    public override Type csharp_type => _csharp_type;

    public MethodInfo setterMI , getterMI; 

    public override object DESER<TMPVAL_Type>(TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> SER_RHS)
    {
        object RES = RuntimeHelpers.GetObjectValue( new TContainer() ); // explicitly  trigger boxing, so that the setter doesn't operate on an implicitly created boxed copy 

        var sequence_view  = SER_RHS.GetSequenceView_RO( _exp_type , arg );
        for ( int i = 0 ; i < len ; i ++ )
        {
            TMPVAL_Type se_elem = sequence_view.Next();
            object des_elem     = element_serializer.DESER(se_elem , SER_RHS);
            setterMI.Invoke(RES,new object [] { i , des_elem});
        }
        return RES;
    }

    public override TMPVAL_Type SER<TMPVAL_Type>(object arg, SerializerChannel_min<TMPVAL_Type> SER_RHS)
    {
        var sequence_builder = SER_RHS.GetSequenceBuilder(_exp_type , len );
        for ( int i = 0  ; i < len ; i ++ )
        {
            var elem = getterMI.Invoke( arg , new object[] { i } );
            sequence_builder.Append( element_serializer.SER( elem , SER_RHS ) ) ;
        }
        return sequence_builder.Final();
    }
}


public class ExpSerializer_IndexerConstLen_Dim2<TContainer,TElement>  : ExpSerializer where TContainer : new() 
{
    public int len_outer , len_inner;
    public ExpSerializer element_serializer;

    public ExpSerializer_IndexerConstLen_Dim2 ( int len_outer , int len_inner , bool only_natural = false )
    {
        this.len_outer = len_outer ;
        this.len_inner = len_inner ;
        element_serializer = ExpType.GetSerializer( typeof(TElement) , only_natural);
        _csharp_type = typeof(TContainer);
        sequence_exp_type    = new SequenceExpType( new SequenceExpType( element_serializer.exp_type ));

        setterMI = typeof(TContainer).GetMethod("set_Item" , new [] { typeof(int) , typeof(int) , typeof(TElement)} ) ;
        getterMI = typeof(TContainer).GetMethod("get_Item" , new [] { typeof(int) , typeof(int) });
    }

    public SequenceExpType sequence_exp_type ;
    public override ExpType exp_type => sequence_exp_type;
    public Type _csharp_type;
    public override Type csharp_type => _csharp_type;

    public MethodInfo setterMI , getterMI; 

    public override object DESER<TMPVAL_Type>(TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> SER_RHS)
    {
        object RES = RuntimeHelpers.GetObjectValue( new TContainer() ); // explicitly  trigger boxing, so that the setter doesn't operate on an implicitly created boxed copy 

        var sequence_view_outer  = SER_RHS.GetSequenceView_RO( sequence_exp_type , arg );
        for ( int i = 0 ; i < len_outer ; i ++ ) { 
            TMPVAL_Type inner_seq_tmp = sequence_view_outer.Next();
            var         sequence_view_inner = SER_RHS.GetSequenceView_RO((SequenceExpType)sequence_exp_type.memberType , inner_seq_tmp) ;
            for ( int j = 0 ; j < len_inner ; j ++ )
            {
                TMPVAL_Type se_elem = sequence_view_inner.Next();
                object des_elem     = element_serializer.DESER(se_elem , SER_RHS);
                setterMI.Invoke(RES,new object [] { i , j , des_elem});
            }
        }
        return RES;
    }

    public override TMPVAL_Type SER<TMPVAL_Type>(object arg, SerializerChannel_min<TMPVAL_Type> SER_RHS)
    {
        var sequence_builder_outer = SER_RHS.GetSequenceBuilder(sequence_exp_type , len_outer );
        for ( int i = 0  ; i < len_outer ; i ++ ) { 
            var sequence_builder_inner = SER_RHS.GetSequenceBuilder((SequenceExpType) sequence_exp_type.memberType , len_inner);

            for ( int j = 0 ; j < len_inner ; j ++)   
            {
                var elem = getterMI.Invoke( arg , new object[] { i , j } );
                sequence_builder_inner.Append( element_serializer.SER( elem , SER_RHS ) ) ;
            }
            sequence_builder_outer.Append( sequence_builder_inner.Final());
        }
        return sequence_builder_outer.Final();
    }

}


public class ExpSerializer_Array_Dim1<ElemT> : ExpSerializer
{
    public override ExpType exp_type => seq_exp_type ;
    public override Type csharp_type => typeof(ElemT[]);

    public SequenceExpType seq_exp_type; 
    public ExpType         elem_exp_type ;
    public ExpSerializer   elem_serializer;

    

    public ExpSerializer_Array_Dim1( bool only_natural = false )
    {
        elem_serializer = ExpType.GetSerializer(typeof(ElemT) , only_natural );
        seq_exp_type    = new SequenceExpType( elem_serializer.exp_type);
    }
    
    public override object DESER<TMPVAL_Type>(TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> SER_RHS)
    {

        var SEQView = SER_RHS.GetSequenceView_RO( seq_exp_type , arg );

        var cnt = SEQView.Count();
        var R = new ElemT[cnt];
        for ( int i = 0 ; i < cnt ; i ++)
        {
            TMPVAL_Type   elm_tmpval = SEQView.Next();
            object        elm_obj    = elem_serializer.DESER<TMPVAL_Type>(elm_tmpval, SER_RHS);
            R[i] = (ElemT)elm_obj;
        }

        return R;
    }

    public override TMPVAL_Type SER<TMPVAL_Type>(object arg, SerializerChannel_min<TMPVAL_Type> SER_RHS)
    {
        var arg_arr = (ElemT[])arg ;
        var child_exp_type = elem_serializer.exp_type;

        var SEQBuilder = SER_RHS.GetSequenceBuilder( seq_exp_type , arg_arr.Length);
        
        foreach ( ElemT csharp_elem in arg_arr)
        {
            TMPVAL_Type ch_elem = elem_serializer.SER( csharp_elem , SER_RHS );
            SEQBuilder.Append(ch_elem);
        }
        return SEQBuilder.Final();
    }
}