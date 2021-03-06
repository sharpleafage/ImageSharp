﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Primitives;
using SixLabors.Primitives;

namespace SixLabors.ImageSharp.Processing.Processors.Convolution
{
    /// <summary>
    /// Applies Gaussian sharpening processing to the image.
    /// </summary>
    /// <typeparam name="TPixel">The pixel format.</typeparam>
    internal class GaussianSharpenProcessor<TPixel> : ImageProcessor<TPixel>
        where TPixel : struct, IPixel<TPixel>
    {
        /// <summary>
        /// The maximum size of the kernel in either direction.
        /// </summary>
        private readonly int kernelSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianSharpenProcessor{TPixel}"/> class.
        /// </summary>
        /// <param name="sigma">
        /// The 'sigma' value representing the weight of the sharpening.
        /// </param>
        public GaussianSharpenProcessor(float sigma = 3F)
        {
            this.kernelSize = ((int)Math.Ceiling(sigma) * 2) + 1;
            this.Sigma = sigma;
            this.KernelX = this.CreateGaussianKernel(true);
            this.KernelY = this.CreateGaussianKernel(false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianSharpenProcessor{TPixel}"/> class.
        /// </summary>
        /// <param name="radius">
        /// The 'radius' value representing the size of the area to sample.
        /// </param>
        public GaussianSharpenProcessor(int radius)
        {
            this.kernelSize = (radius * 2) + 1;
            this.Sigma = radius;
            this.KernelX = this.CreateGaussianKernel(true);
            this.KernelY = this.CreateGaussianKernel(false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianSharpenProcessor{TPixel}"/> class.
        /// </summary>
        /// <param name="sigma">
        /// The 'sigma' value representing the weight of the sharpen.
        /// </param>
        /// <param name="radius">
        /// The 'radius' value representing the size of the area to sample.
        /// This should be at least twice the sigma value.
        /// </param>
        public GaussianSharpenProcessor(float sigma, int radius)
        {
            this.kernelSize = (radius * 2) + 1;
            this.Sigma = sigma;
            this.KernelX = this.CreateGaussianKernel(true);
            this.KernelY = this.CreateGaussianKernel(false);
        }

        /// <summary>
        /// Gets the sigma value representing the weight of the blur
        /// </summary>
        public float Sigma { get; }

        /// <summary>
        /// Gets the horizontal gradient operator.
        /// </summary>
        public DenseMatrix<float> KernelX { get; }

        /// <summary>
        /// Gets the vertical gradient operator.
        /// </summary>
        public DenseMatrix<float> KernelY { get; }

        /// <inheritdoc/>
        protected override void OnFrameApply(ImageFrame<TPixel> source, Rectangle sourceRectangle, Configuration configuration)
        {
            new Convolution2PassProcessor<TPixel>(this.KernelX, this.KernelY).Apply(source, sourceRectangle, configuration);
        }

        /// <summary>
        /// Create a 1 dimensional Gaussian kernel using the Gaussian G(x) function
        /// </summary>
        /// <param name="horizontal">Whether to calculate a horizontal kernel.</param>
        /// <returns>The <see cref="DenseMatrix{T}"/></returns>
        private DenseMatrix<float> CreateGaussianKernel(bool horizontal)
        {
            int size = this.kernelSize;
            float weight = this.Sigma;
            DenseMatrix<float> kernel = horizontal
                ? new DenseMatrix<float>(size, 1)
                : new DenseMatrix<float>(1, size);

            float sum = 0;

            float midpoint = (size - 1) / 2f;
            for (int i = 0; i < size; i++)
            {
                float x = i - midpoint;
                float gx = ImageMaths.Gaussian(x, weight);
                sum += gx;
                if (horizontal)
                {
                    kernel[0, i] = gx;
                }
                else
                {
                    kernel[i, 0] = gx;
                }
            }

            // Invert the kernel for sharpening.
            int midpointRounded = (int)midpoint;

            if (horizontal)
            {
                for (int i = 0; i < size; i++)
                {
                    if (i == midpointRounded)
                    {
                        // Calculate central value
                        kernel[0, i] = (2F * sum) - kernel[0, i];
                    }
                    else
                    {
                        // invert value
                        kernel[0, i] = -kernel[0, i];
                    }
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    if (i == midpointRounded)
                    {
                        // Calculate central value
                        kernel[i, 0] = (2 * sum) - kernel[i, 0];
                    }
                    else
                    {
                        // invert value
                        kernel[i, 0] = -kernel[i, 0];
                    }
                }
            }

            // Normalize kernel so that the sum of all weights equals 1
            if (horizontal)
            {
                for (int i = 0; i < size; i++)
                {
                    kernel[0, i] /= sum;
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    kernel[i, 0] /= sum;
                }
            }

            return kernel;
        }
    }
}