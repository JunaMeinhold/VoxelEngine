// DirectXTexNet.h

#pragma once

//#using "DirectXTexNet.dll" as_friend

#define API __declspec(dllexport)

API size_t GetImageCount(DirectX::ScratchImage* image)
{
	return image->GetImageCount();
}

API size_t ComputeImageIndex(DirectX::ScratchImage* image, size_t mip, size_t item, size_t slice)
{
	return image->GetMetadata().ComputeIndex(mip, item, slice);
}

/*
API DirectX::Image* GetImage(size_t index)
{
	return *GetImageInternal(index);
}
*/

API const DirectX::Image* GetImage(DirectX::ScratchImage* image, size_t mip, size_t item, size_t slice)
{
	return image->GetImage(mip, item, slice);
}

API const DirectX::TexMetadata GetMetadata(DirectX::ScratchImage* image)
{
	return image->GetMetadata();
}

// creating image copies
API DirectX::ScratchImage* CreateImageCopy(DirectX::ScratchImage* image, size_t imageIndex, bool allow1D, DirectX::CP_FLAGS flags)
{
	DirectX::Resize(&image, )
}

API DirectX::ScratchImage* CreateArrayCopy(DirectX::ScratchImage* image, size_t startIndex, size_t nImages, bool allow1D, DirectX::CP_FLAGS flags)
{
}

API DirectX::ScratchImage* CreateCubeCopy(DirectX::ScratchImage* image, size_t startIndex, size_t nImages, DirectX::CP_FLAGS flags)
{
}

API DirectX::ScratchImage* CreateVolumeCopy(DirectX::ScratchImage* image, size_t startIndex, size_t depth, DirectX::CP_FLAGS flags)
{
}

namespace DirectXTexNet
{
	typedef void(__cdecl* EvaluatePixelsFunctionDeclaration)(_In_reads_(width) const DirectX::XMVECTOR* pixels, size_t width, size_t y);

	typedef void(__cdecl* TransformPixelsFunctionDeclaration)(_Out_writes_(width) DirectX::XMVECTOR* outPixels, _In_reads_(width) const DirectX::XMVECTOR* inPixels, size_t width, size_t y);

	class ScratchImageImpl
	{
		virtual size_t GetImageCountInternal() = 0;
		virtual const DirectX::Image* GetImagesInternal() = 0;
		virtual const DirectX::TexMetadata& GetMetadataInternal() = 0;

		const DirectX::Image* GetImageInternal(size_t index)
		{
			if (index >= GetImageCountInternal())
				assert("The image index is out of range.");
			return &GetImagesInternal()[index];
		}

		const DirectX::Image* GetImageInternal(size_t mip, size_t item, size_t slice)
		{
			size_t index = this->GetMetadataInternal().ComputeIndex(mip, item, slice);
			return GetImageInternal(index);
		}

	public:

		size_t ComputeImageIndex(size_t mip, size_t item, size_t slice)
		{
			return static_cast<size_t>(this->GetMetadataInternal().ComputeIndex(mip, item, slice));
		}

		DirectX::Image* GetImage(size_t index)
		{
			return *GetImageInternal(index);
		}

		DirectX::Image* GetImage(size_t mip, size_t item, size_t slice)
		{
			return *GetImageInternal(mip, item, slice);
		}

		DirectX::TexMetadata* GetMetadata()
		{
			return this->GetMetadataInternal();
		}

		// creating image copies
		DirectX::ScratchImage* CreateImageCopy(size_t imageIndex, bool allow1D, DirectX::CP_FLAGS flags);

		DirectX::ScratchImage* CreateArrayCopy(size_t startIndex, size_t nImages, bool allow1D, DirectX::CP_FLAGS flags);

		DirectX::ScratchImage* CreateCubeCopy(size_t startIndex, size_t nImages, DirectX::CP_FLAGS flags);

		DirectX::ScratchImage* CreateVolumeCopy(size_t startIndex, size_t depth, DirectX::CP_FLAGS flags);

		// saving images to file/memory
		void SaveToDDSMemory(size_t imageIndex, DirectX::DDS_FLAGS flags, [OUT] void* pData, [OUT] size_t pSize);

		void SaveToDDSMemory(DirectX::DDS_FLAGS flags, [OUT] void* pData, [OUT] size_t pSize);

		void SaveToDDSFile(size_t imageIndex, DirectX::DDS_FLAGS flags, char16_t* szFile);

		void SaveToDDSFile(DirectX::DDS_FLAGS flags, char16_t* szFile);

		void SaveToHDRMemory(size_t imageIndex, [OUT] void* pData, [OUT] size_t pSize);

		void SaveToHDRFile(size_t imageIndex, char16_t* szFile);

		void SaveToTGAMemory(size_t imageIndex, [OUT] void* pData, [OUT] size_t pSize);

		void SaveToTGAFile(size_t imageIndex, char16_t* szFile);

		void SaveToWICMemory(size_t imageIndex, DirectX::WIC_FLAGS flags, GUID guidContainerFormat, [OUT] void* pData, [OUT] size_t pSize);

		void SaveToWICMemory(size_t startIndex, size_t nImages, DirectX::WIC_FLAGS flags, GUID guidContainerFormat, [OUT] void* pData, [OUT] size_t pSize);

		void SaveToWICFile(size_t imageIndex, DirectX::WIC_FLAGS flags, GUID guidContainerFormat, char16_t* szFile);

		void SaveToWICFile(size_t startIndex, size_t nImages, DirectX::WIC_FLAGS flags, GUID guidContainerFormat, char16_t* szFile);

		void SaveToJPGMemory(size_t imageIndex, float quality, [OUT] void* pData, [OUT] size_t pSize);

		void SaveToJPGFile(size_t imageIndex, float quality, char16_t* szFile);

		// Texture conversion, resizing, mipmap generation, and block compression
		DirectX::ScratchImage* FlipRotate(size_t imageIndex, DirectX::TEX_FR_FLAGS flags);

		DirectX::ScratchImage* FlipRotate(DirectX::TEX_FR_FLAGS flags);

		DirectX::ScratchImage* Resize(size_t imageIndex, size_t width, size_t height, DirectX::TEX_FILTER_FLAGS filter);

		DirectX::ScratchImage* Resize(size_t width, size_t height, DirectX::TEX_FILTER_FLAGS filter);

		DirectX::ScratchImage* Convert(size_t imageIndex, DXGI_FORMAT format, DirectX::TEX_FILTER_FLAGS filter, float threshold);

		DirectX::ScratchImage* Convert(DXGI_FORMAT format, DirectX::TEX_FILTER_FLAGS filter, float threshold);

		DirectX::ScratchImage* ConvertToSinglePlane(size_t imageIndex);

		DirectX::ScratchImage* ConvertToSinglePlane();

		DirectX::ScratchImage* CreateCopyWithEmptyMipMaps(size_t levels, DXGI_FORMAT fmt, DirectX::CP_FLAGS flags, bool zeroOutMipMaps);

		DirectX::ScratchImage* GenerateMipMaps(size_t imageIndex, DirectX::TEX_FILTER_FLAGS filter, size_t levels, bool allow1D);

		DirectX::ScratchImage* GenerateMipMaps(DirectX::TEX_FILTER_FLAGS filter, size_t levels);

		DirectX::ScratchImage* GenerateMipMaps3D(size_t startIndex, size_t depth, DirectX::TEX_FILTER_FLAGS filter, size_t levels);

		DirectX::ScratchImage* GenerateMipMaps3D(DirectX::TEX_FILTER_FLAGS filter, size_t levels);

		DirectX::ScratchImage* PremultiplyAlpha(size_t imageIndex, DirectX::TEX_PMALPHA_FLAGS flags);

		DirectX::ScratchImage* PremultiplyAlpha(DirectX::TEX_PMALPHA_FLAGS flags);

		DirectX::ScratchImage* Compress(size_t imageIndex, DXGI_FORMAT format, DirectX::TEX_COMPRESS_FLAGS compress, float threshold);

		DirectX::ScratchImage* Compress(DXGI_FORMAT format, DirectX::TEX_COMPRESS_FLAGS compress, float threshold);

		DirectX::ScratchImage* Compress(size_t imageIndex, ID3D11Device* pDevice, DXGI_FORMAT format, DirectX::TEX_COMPRESS_FLAGS compress, float alphaWeight);

		DirectX::ScratchImage* Compress(ID3D11Device* pDevice, DXGI_FORMAT format, DirectX::TEX_COMPRESS_FLAGS compress, float alphaWeight);

		DirectX::ScratchImage* Decompress(size_t imageIndex, DXGI_FORMAT format);

		DirectX::ScratchImage* Decompress(DXGI_FORMAT format);

		// Normal map operations
		DirectX::ScratchImage* ComputeNormalMap(size_t imageIndex, DirectX::CNMAP_FLAGS flags, float amplitude, DXGI_FORMAT format);

		DirectX::ScratchImage* ComputeNormalMap(DirectX::CNMAP_FLAGS flags, float amplitude, DXGI_FORMAT format);

		// Misc image operations
		void EvaluateImage(size_t imageIndex, EvaluatePixelsDelegate* pixelFunc);

		void EvaluateImage(EvaluatePixelsDelegate* pixelFunc);

		DirectX::ScratchImage* TransformImage(size_t imageIndex, TransformPixelsDelegate* pixelFunc);

		DirectX::ScratchImage* TransformImage(TransformPixelsDelegate* pixelFunc);

		// Direct3D 11 functions
		ID3D11Resource* CreateTexture(ID3D11Device* pDevice);

		ID3D11ShaderResourceView* CreateShaderResourceView(ID3D11Device* pDevice);

		ID3D11Resource* CreateTextureEx(
			ID3D11Device* pDevice,
			D3D11_USAGE usage,
			D3D11_BIND_FLAG bindFlags,
			D3D11_CPU_ACCESS_FLAG cpuAccessFlags,
			D3D11_RESOURCE_MISC_FLAG miscFlags,
			bool forceSRGB);

		ID3D11ShaderResourceView* CreateShaderResourceViewEx(
			ID3D11Device* pDevice,
			D3D11_USAGE usage,
			D3D11_BIND_FLAG bindFlags,
			D3D11_CPU_ACCESS_FLAG cpuAccessFlags,
			D3D11_RESOURCE_MISC_FLAG miscFlags,
			bool forceSRGB);
	};

	class TempScratchImageImpl : ScratchImageImpl
	{
	public:

		bool OverrideFormat(DXGI_FORMAT newFormat)
		{
			auto f = static_cast<::DXGI_FORMAT>(newFormat);

			if (!m_image)
				return false;

			if (!DirectX::IsValid(f) || DirectX::IsPlanar(f) || DirectX::IsPalettized(f))
				return false;

			for (size_t index = 0; index < m_nimages; ++index)
			{
				m_image[index].format = f;
			}

			m_metadata->format = f;

			return true;
		}

		bool OwnsData()
		{
			return false;
		}

		void* GetPixels();

		size_t GetPixelsSize()
		{
			return size_t(-1);
		}

		bool IsAlphaAllOpaque()
		{
			//throw new NotSupportedException("Not supported by temporary ScratchImage.");
		}

		~TempScratchImageImpl()
		{
			if (otherDisposables != nullptr)
			{
				for (int i = 0; i < otherDisposables->Length; i++)
				{
					delete otherDisposables[i];
				}
				otherDisposables = nullptr;
			}
			this->TempScratchImageImpl();
		}

	protected:
		TempScratchImageImpl()
		{
			otherDisposables = nullptr;
			origImages = nullptr;
			m_nimages = 0;
			if (this->m_metadata != nullptr)
			{
				delete m_metadata;
				m_metadata = nullptr;
			}
			if (this->m_image != nullptr)
			{
				delete[] m_image;
				m_image = nullptr;
			}
		}

		size_t GetImageCountInternal()
		{
			return m_nimages;
		}

		const DirectX::Image* GetImagesInternal()
		{
			return m_image;
		}

		const DirectX::TexMetadata& GetMetadataInternal()
		{
			return *m_metadata;
		}

		TempScratchImageImpl(array<Image*>^ _images, DirectX::TexMetadata* _metadata, array<IDisposable^>^ takeOwnershipOf)
		{
			m_metadata = new DirectX::TexMetadata;
			FromManaged(_metadata, *m_metadata);

			int length = _images->Length;
			m_nimages = static_cast<size_t>(length);
			m_image = new DirectX::Image[length];

			origImages = gcnew array<Image*>(length);

			for (int i = 0; i < length; i++)
			{
				Image* origImage = _images[i];
				origImages[i] = origImage;
				FromManaged(origImage, m_image[i]);
			}

			if (takeOwnershipOf != nullptr)
			{
				otherDisposables = gcnew array<IDisposable^>(takeOwnershipOf->Length);
				for (int i = 0; i < otherDisposables->Length; i++)
				{
					otherDisposables[i] = takeOwnershipOf[i];
				}
			}
		}

	private:
		array<Image*>^ origImages;
		array<IDisposable^>^ otherDisposables;

		size_t                m_nimages;
		DirectX::TexMetadata* m_metadata;
		DirectX::Image* m_image;
	};

	class ActualScratchImageImpl : ScratchImageImpl
	{
	public:
		bool OverrideFormat(DXGI_FORMAT f)
		{
			return scratchImage_->OverrideFormat(static_cast<::DXGI_FORMAT>(f));
		}

		bool OwnsData()
		{
			return false;
		}

		void* GetPixels()
		{
			return scratchImage_->GetPixels();
		}

		size_t GetPixelsSize()
		{
			return scratchImage_->GetPixelsSize();
		}

		bool IsAlphaAllOpaque()
		{
			return scratchImage_->IsAlphaAllOpaque();
		}

	protected:
		ActualScratchImageImpl()
		{
			if (this->scratchImage_ != nullptr)
			{
				delete scratchImage_;
				scratchImage_ = nullptr;
			}
		}

		size_t GetImageCountInternal()
		{
			return scratchImage_->GetImageCount();
		}

		const DirectX::Image* GetImagesInternal()
		{
			return scratchImage_->GetImages();
		}

		const DirectX::TexMetadata& GetMetadataInternal()
		{
			return scratchImage_->GetMetadata();
		}

		ActualScratchImageImpl()
		{
			scratchImage_ = new DirectX::ScratchImage();
		}

		DirectX::ScratchImage* scratchImage_;
	};

	class TexHelper
	{
	public:

#ifdef _OPENMP
		void SetOmpMaxThreadCount(int maxThreadCount);
#endif

		// DXGI Format Utilities
		bool IsValid(DXGI_FORMAT fmt);

		bool IsCompressed(DXGI_FORMAT fmt);

		bool IsPacked(DXGI_FORMAT fmt);

		bool IsVideo(DXGI_FORMAT fmt);

		bool IsPlanar(DXGI_FORMAT fmt);

		bool IsPalettized(DXGI_FORMAT fmt);

		bool IsDepthStencil(DXGI_FORMAT fmt);

		bool IsSRGB(DXGI_FORMAT fmt);

		bool IsTypeless(DXGI_FORMAT fmt, bool partialTypeless);

		bool HasAlpha(DXGI_FORMAT fmt);

		size_t BitsPerPixel(DXGI_FORMAT fmt);

		size_t BitsPerColor(DXGI_FORMAT fmt);

		void ComputePitch(DXGI_FORMAT fmt, size_t width, size_t height, [Out] size_t rowPitch, [Out] size_t slicePitch, CP_FLAGS flags);

		size_t ComputeScanlines(DXGI_FORMAT fmt, size_t height);

		size_t ComputeImageIndex(DirectX::TexMetadata* metadata, size_t mip, size_t item, size_t slice);

		DXGI_FORMAT MakeSRGB(DXGI_FORMAT fmt);

		DXGI_FORMAT MakeTypeless(DXGI_FORMAT fmt);

		DXGI_FORMAT MakeTypelessUNORM(DXGI_FORMAT fmt);

		DXGI_FORMAT MakeTypelessFLOAT(DXGI_FORMAT fmt);

		// Get Texture metadata
		DirectX::TexMetadata* GetMetadataFromDDSMemory(void* pSource, size_t size, DirectX::DDS_FLAGS flags);

		DirectX::TexMetadata* GetMetadataFromDDSFile(char16_t* szFile, DirectX::DDS_FLAGS flags);

		DirectX::TexMetadata* GetMetadataFromHDRMemory(void* pSource, size_t size);

		DirectX::TexMetadata* GetMetadataFromHDRFile(char16_t* szFile);

		DirectX::TexMetadata* GetMetadataFromTGAMemory(void* pSource, size_t size);

		DirectX::TexMetadata* GetMetadataFromTGAFile(char16_t* szFile);

		DirectX::TexMetadata* GetMetadataFromWICMemory(void* pSource, size_t size, DirectX::WIC_FLAGS flags);

		DirectX::TexMetadata* GetMetadataFromWICFile(char16_t* szFile, DirectX::WIC_FLAGS flags);

		// create new ScratchImages
		DirectX::ScratchImage* Initialize(DirectX::TexMetadata* _metadata, DirectX::CP_FLAGS flags);

		DirectX::ScratchImage* Initialize1D(DXGI_FORMAT fmt, size_t length, size_t arraySize, size_t mipLevels, DirectX::CP_FLAGS flags);

		DirectX::ScratchImage* Initialize2D(DXGI_FORMAT fmt, size_t width, size_t height, size_t arraySize, size_t mipLevels, DirectX::CP_FLAGS flags);

		DirectX::ScratchImage* Initialize3D(DXGI_FORMAT fmt, size_t width, size_t height, size_t depth, size_t mipLevels, DirectX::CP_FLAGS flags);

		DirectX::ScratchImage* InitializeCube(DXGI_FORMAT fmt, size_t width, size_t height, size_t nCubes, size_t mipLevels, DirectX::CP_FLAGS flags);

		// Load Images
		DirectX::ScratchImage* LoadFromDDSMemory(void* pSource, size_t size, DirectX::DDS_FLAGS flags);

		DirectX::ScratchImage* LoadFromDDSFile(char16_t* szFile, DirectX::DDS_FLAGS flags);

		DirectX::ScratchImage* LoadFromHDRMemory(void* pSource, size_t size);

		DirectX::ScratchImage* LoadFromHDRFile(char16_t* szFile);

		DirectX::ScratchImage* LoadFromTGAMemory(void* pSource, size_t size);

		DirectX::ScratchImage* LoadFromTGAFile(char16_t* filename);

		DirectX::ScratchImage* LoadFromWICMemory(void* pSource, size_t size, DirectX::WIC_FLAGS flags);

		DirectX::ScratchImage* LoadFromWICFile(char16_t* filename, DirectX::WIC_FLAGS flags);

		// Misc image operations
		void CopyRectangle(
			DirectX::Image* srcImage,
			size_t srcX,
			size_t srcY,
			size_t srcWidth,
			size_t srcHeight,
			DirectX::Image* dstImage,
			DirectX::TEX_FILTER_FLAGS filter,
			size_t xOffset,
			size_t yOffset);

		// WIC utility
		GUID GetWICCodec(DirectX::WICCodecs codec);

		IWICImagingFactory* GetWICFactory(bool iswic2);

		void SetWICFactory(IWICImagingFactory* pWIC);

		// Direct3D 11 functions
		bool IsSupportedTexture(ID3D11Device* pDevice, DirectX::TexMetadata* _metadata);

		DirectX::ScratchImage* CaptureTexture(ID3D11Device* pDevice, ID3D11DeviceContext* pContext, ID3D11Resource* pSource);
	};
}
