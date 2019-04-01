#ifndef Base_Unknown_H
#define Base_Unknown_H

#include "Buffer.h"
#include "Identities.h"
#include "MessageBroker.h"
#include "Protocol.h"
#include "RdId.h"
#include "RdList.h"
#include "RdMap.h"
#include "RdProperty.h"
#include "RdSet.h"
#include "RdSignal.h"
#include "RName.h"
#include "ISerializable.h"
#include "Polymorphic.h"
#include "NullableSerializer.h"
#include "ArraySerializer.h"
#include "InternedSerializer.h"
#include "SerializationCtx.h"
#include "Serializers.h"
#include "ISerializersOwner.h"
#include "IUnknownInstance.h"
#include "RdExtBase.h"
#include "RdCall.h"
#include "RdEndpoint.h"
#include "RdTask.h"
#include "gen_util.h"

#include <iostream>
#include <cstring>
#include <cstdint>
#include <vector>
#include <type_traits>
#include <utility>

#include "optional.hpp"

#include "Base.h"


#pragma warning( push )
#pragma warning( disable:4250 )
#pragma warning( disable:4307 )
namespace demo {
    class Base_Unknown : public Base, public rd::IUnknownInstance
    {
        
        //companion
        friend class Base;
        
        //custom serializers
        private:
        
        //fields
        protected:
        rd::RdId unknownId_;
        rd::Buffer::ByteArray unknownBytes_;
        
        
        //initializer
        private:
        void initialize();
        
        //primary ctor
        public:
        Base_Unknown(rd::RdId unknownId_, rd::Buffer::ByteArray unknownBytes_);
        
        //secondary constructor
        
        //default ctors and dtors
        
        Base_Unknown();
        
        Base_Unknown(Base_Unknown const &) = default;
        
        Base_Unknown& operator=(Base_Unknown const &) = default;
        
        Base_Unknown(Base_Unknown &&) = default;
        
        Base_Unknown& operator=(Base_Unknown &&) = default;
        
        virtual ~Base_Unknown() = default;
        
        //reader
        static Base_Unknown read(rd::SerializationCtx const& ctx, rd::Buffer const & buffer);
        
        //writer
        void write(rd::SerializationCtx const& ctx, rd::Buffer const& buffer) const override;
        
        //virtual init
        
        //identify
        
        //getters
        
        //intern
        
        //equals trait
        private:
        bool equals(rd::ISerializable const& object) const override;
        
        //equality operators
        public:
        friend bool operator==(const Base_Unknown &lhs, const Base_Unknown &rhs);
        friend bool operator!=(const Base_Unknown &lhs, const Base_Unknown &rhs);
        
        //hash code trait
        size_t hashCode() const override;
        
        //type name trait
        std::string type_name() const override;
    };
};

#pragma warning( pop )


//hash code trait
namespace std {
    template <> struct hash<demo::Base_Unknown> {
        size_t operator()(const demo::Base_Unknown & value) const {
            return value.hashCode();
        }
    };
}

#endif // Base_Unknown_H